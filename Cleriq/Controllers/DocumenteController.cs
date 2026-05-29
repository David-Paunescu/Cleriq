using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Helpers;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumenteController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IStocareDocumente _stocare;

    public DocumenteController(AppDbContext context, IStocareDocumente stocare)
    {
        _context = context;
        _stocare = stocare;
    }

    [HttpGet]
    public async Task<IActionResult> Lista([FromQuery] int? sedintaId, [FromQuery] int? punctId)
    {
        if (sedintaId.HasValue == punctId.HasValue)
            return BadRequest("Furnizează exact unul dintre sedintaId sau punctId.");

        var query = _context.Documente.AsQueryable();
        if (sedintaId.HasValue) query = query.Where(d => d.SedintaId == sedintaId.Value);
        if (punctId.HasValue) query = query.Where(d => d.PunctId == punctId.Value);

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
        [FromForm] string? descriere,
        [FromForm] int ordine,
        CancellationToken ct)
    {
        if (fisier is null || fisier.Length == 0)
            return BadRequest("Fișier lipsă.");

        if (sedintaId.HasValue == punctId.HasValue)
            return BadRequest("Documentul trebuie atașat la exact unul dintre sedintaId sau punctId.");

        if (string.IsNullOrWhiteSpace(denumire))
            return BadRequest("Denumirea este obligatorie.");

        var eroare = ValidareDocument.Valideaza(fisier.FileName, fisier.Length);
        if (eroare is not null) return BadRequest(eroare);

        if (sedintaId.HasValue)
        {
            var existaSedinta = await _context.Sedinte.AnyAsync(s => s.Id == sedintaId.Value, ct);
            if (!existaSedinta) return NotFound("Ședința nu există.");
        }
        else
        {
            var existaPunct = await _context.PuncteOrdineZi.AnyAsync(p => p.Id == punctId!.Value, ct);
            if (!existaPunct) return NotFound("Punctul nu există.");
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
            TipDocument = tipDocument,
            NumeFisierOriginal = Path.GetFileName(fisier.FileName),
            TipMime = ValidareDocument.TipMimePentru(fisier.FileName),
            Marime = stocat.Marime,
            HashSha256 = stocat.HashSha256,
            CaleStocare = stocat.Cheie,
            EstePublic = false,
            Ordine = ordine,
            SedintaId = sedintaId,
            PunctId = punctId
        };

        _context.Documente.Add(doc);
        await _context.SaveChangesAsync(ct);

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
        doc.TipDocument = dto.TipDocument;
        doc.Ordine = dto.Ordine;

        await _context.SaveChangesAsync();
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

    private static DocumentDto MapeazaSpreDto(Document d) => new(
        d.Id, d.Denumire, d.Descriere, d.TipDocument,
        d.NumeFisierOriginal, d.TipMime, d.Marime, d.HashSha256,
        d.EstePublic, d.Ordine, d.SedintaId, d.PunctId, d.CreatLa);
}