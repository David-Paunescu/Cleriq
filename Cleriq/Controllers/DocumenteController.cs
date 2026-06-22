using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Helpers;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumenteController : ControllerBase
{
    private const int SqlServerErrorUniqueConstraint = 2601;
    private const int SqlServerErrorDuplicateKey = 2627;

    private readonly AppDbContext _context;
    private readonly IStocareDocumente _stocare;

    public DocumenteController(AppDbContext context, IStocareDocumente stocare)
    {
        _context = context;
        _stocare = stocare;
    }

    [HttpGet]
    public async Task<IActionResult> Lista(
        [FromQuery] int? sedintaId, [FromQuery] int? punctId, [FromQuery] int? hclId)
    {
        var contexte = new[] { sedintaId.HasValue, punctId.HasValue, hclId.HasValue }.Count(x => x);
        if (contexte != 1)
            return BadRequest("Furnizează exact unul dintre sedintaId, punctId sau hclId.");

        var query = _context.Documente.AsQueryable();
        if (sedintaId.HasValue) query = query.Where(d => d.SedintaId == sedintaId.Value);
        else if (punctId.HasValue) query = query.Where(d => d.PunctId == punctId.Value);
        else query = query.Where(d => d.HclId == hclId!.Value);

        var rezultat = await query
            .OrderBy(d => d.Ordine).ThenBy(d => d.CreatLa)
            .Select(d => MapeazaSpreDto(d))
            .ToListAsync();

        return Ok(rezultat);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalii(int id)
    {
        var doc = await _context.Documente.FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return NotFound();
        return Ok(MapeazaSpreDto(doc));
    }

    [HttpGet("{id}/Continut")]
    public async Task<IActionResult> Descarca(int id, CancellationToken ct)
    {
        var doc = await _context.Documente.FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return NotFound();

        Stream stream;
        try
        {
            stream = await _stocare.DeschideAsync(doc.CaleStocare, ct);
        }
        catch (FileNotFoundException)
        {
            return NotFound("Fișierul fizic lipsește.");
        }

        return File(stream, doc.TipMime, doc.NumeFisierOriginal);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Secretar")]
    [RequestSizeLimit(ValidareDocument.MarimeMaxima)]
    public async Task<IActionResult> Incarca(
        [FromForm] IFormFile fisier,
        [FromForm] string denumire,
        [FromForm] TipDocument tipDocument,
        [FromForm] int? sedintaId,
        [FromForm] int? punctId,
        [FromForm] int? hclId,
        [FromForm] TipDocumentHcl? tipDocumentHcl,
        [FromForm] int? numarOrdinAnexa,
        [FromForm] string? descriere,
        [FromForm] int ordine,
        CancellationToken ct)
    {
        if (fisier is null || fisier.Length == 0)
            return BadRequest("Fișier lipsă.");

        var contexte = new[] { sedintaId.HasValue, punctId.HasValue, hclId.HasValue }.Count(x => x);
        if (contexte != 1)
            return BadRequest("Documentul trebuie atașat la exact unul dintre sedintaId, punctId sau hclId.");

        if (string.IsNullOrWhiteSpace(denumire))
            return BadRequest("Denumirea este obligatorie.");

        var eroare = ValidareDocument.Valideaza(fisier.FileName, fisier.Length);
        if (eroare is not null) return BadRequest(eroare);

        // Existență context
        if (sedintaId.HasValue)
        {
            if (!await _context.Sedinte.AnyAsync(s => s.Id == sedintaId.Value, ct))
                return NotFound("Ședința nu există.");
        }
        else if (punctId.HasValue)
        {
            if (!await _context.PuncteOrdineZi.AnyAsync(p => p.Id == punctId.Value, ct))
                return NotFound("Punctul nu există.");
        }
        else
        {
            if (!await _context.Hcluri.AnyAsync(h => h.Id == hclId!.Value, ct))
                return NotFound("HCL-ul nu există.");
        }

        // Coexistență TipDocument vs TipDocumentHcl
        var tipDocFinal = tipDocument;
        TipDocumentHcl? tipDocHclFinal = null;
        int? numarAnexaFinal = null;

        if (hclId.HasValue)
        {
            tipDocFinal = TipDocument.Altele;     // forțat — sursa de adevăr e TipDocumentHcl
            tipDocHclFinal = tipDocumentHcl;

            if (tipDocumentHcl == TipDocumentHcl.Anexa)
            {
                if (numarOrdinAnexa is null)
                    return BadRequest("Pentru o anexă, NumarOrdinAnexa este obligatoriu.");
                numarAnexaFinal = numarOrdinAnexa;

                var dublura = await _context.Documente.AnyAsync(d =>
                    d.HclId == hclId.Value
                    && d.TipDocumentHcl == TipDocumentHcl.Anexa
                    && d.NumarOrdinAnexa == numarOrdinAnexa.Value, ct);
                if (dublura)
                    return Conflict($"Există deja o anexă cu numărul de ordine {numarOrdinAnexa} pe acest HCL.");
            }
        }
        else if (tipDocumentHcl.HasValue)
        {
            return BadRequest("TipDocumentHcl se poate seta doar pentru documente atașate unui HCL.");
        }

        FisierStocat stocat;
        await using (var stream = fisier.OpenReadStream())
        {
            stocat = await _stocare.SalveazaAsync(
                _context.InstitutieIdCurenta, fisier.FileName, stream, ct);
        }

        var doc = new Document
        {
            Denumire = denumire.Trim(),
            Descriere = descriere?.Trim(),
            TipDocument = tipDocFinal,
            NumeFisierOriginal = Path.GetFileName(fisier.FileName),
            TipMime = ValidareDocument.TipMimePentru(fisier.FileName),
            Marime = stocat.Marime,
            HashSha256 = stocat.HashSha256,
            CaleStocare = stocat.Cheie,
            EstePublic = false,
            Ordine = ordine,
            SedintaId = sedintaId,
            PunctId = punctId,
            HclId = hclId,
            TipDocumentHcl = tipDocHclFinal,
            NumarOrdinAnexa = numarAnexaFinal
        };

        _context.Documente.Add(doc);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (EsteViolareUnique(ex))
        {
            return Conflict($"Există deja o anexă cu numărul de ordine {numarOrdinAnexa} pe acest HCL.");
        }

        return Ok(MapeazaSpreDto(doc));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Actualizeaza(int id, ActualizareDocumentDto dto)
    {
        var doc = await _context.Documente.FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Denumire))
            return BadRequest("Denumirea este obligatorie.");

        doc.Denumire = dto.Denumire.Trim();
        doc.Descriere = dto.Descriere?.Trim();
        doc.Ordine = dto.Ordine;

        if (doc.HclId.HasValue)
        {
            var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == doc.HclId.Value);
            if (hcl is null) return NotFound("HCL-ul asociat documentului nu există.");

            // NumarOrdinAnexa imutabil după semnare (referențiat textual în corpul HCL)
            if (hcl.Status == StatusHclRedactional.Semnat && dto.NumarOrdinAnexa != doc.NumarOrdinAnexa)
                return Conflict("HCL semnat — numărul de ordine al anexei nu mai poate fi schimbat (referențiat în corpul hotărârii).");

            if (dto.TipDocumentHcl == TipDocumentHcl.Anexa && dto.NumarOrdinAnexa is null)
                return BadRequest("Pentru o anexă, NumarOrdinAnexa este obligatoriu.");

            if (dto.TipDocumentHcl == TipDocumentHcl.Anexa && dto.NumarOrdinAnexa.HasValue)
            {
                var dublura = await _context.Documente.AnyAsync(d =>
                    d.HclId == doc.HclId.Value
                    && d.TipDocumentHcl == TipDocumentHcl.Anexa
                    && d.NumarOrdinAnexa == dto.NumarOrdinAnexa.Value
                    && d.Id != doc.Id);
                if (dublura)
                    return Conflict($"Există deja o anexă cu numărul de ordine {dto.NumarOrdinAnexa} pe acest HCL.");
            }

            doc.TipDocument = TipDocument.Altele;   // rămâne forțat
            doc.TipDocumentHcl = dto.TipDocumentHcl;
            doc.NumarOrdinAnexa = dto.TipDocumentHcl == TipDocumentHcl.Anexa ? dto.NumarOrdinAnexa : null;
        }
        else
        {
            doc.TipDocument = dto.TipDocument;   // document non-HCL: comportament clasic
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsteViolareUnique(ex))
        {
            return Conflict($"Există deja o anexă cu numărul de ordine {dto.NumarOrdinAnexa} pe acest HCL.");
        }

        return Ok(MapeazaSpreDto(doc));
    }

    [HttpPut("{id}/Vizibilitate")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> SeteazaVizibilitate(int id, SetareVizibilitateDto dto)
    {
        var doc = await _context.Documente.FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return NotFound();

        doc.EstePublic = dto.EstePublic;
        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(doc));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Sterge(int id)
    {
        var doc = await _context.Documente.FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null) return NotFound();

        _context.Documente.Remove(doc);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static bool EsteViolareUnique(DbUpdateException ex)
        => ex.InnerException is SqlException sql
           && (sql.Number == SqlServerErrorUniqueConstraint
               || sql.Number == SqlServerErrorDuplicateKey);

    private static DocumentDto MapeazaSpreDto(Document d) => new(
        d.Id, d.Denumire, d.Descriere, d.TipDocument,
        d.NumeFisierOriginal, d.TipMime, d.Marime, d.HashSha256,
        d.EstePublic, d.Ordine, d.SedintaId, d.PunctId, d.CreatLa,
        d.HclId, d.TipDocumentHcl, d.NumarOrdinAnexa);
}