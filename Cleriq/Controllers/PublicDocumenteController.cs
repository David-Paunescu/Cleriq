using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("public/{slug}")]
public class PublicDocumenteController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IStocareDocumente _stocare;

    public PublicDocumenteController(AppDbContext context, IStocareDocumente stocare)
    {
        _context = context;
        _stocare = stocare;
    }

    [HttpGet("sedinte/{sedintaId:int}/documente")]
    public async Task<IActionResult> ListaPentruSedinta(int sedintaId)
    {
        if (!await SedintaPublica(sedintaId)) return NotFound();

        var rezultat = await _context.Documente
            .Where(d => d.SedintaId == sedintaId && d.EstePublic)
            .OrderBy(d => d.Ordine).ThenBy(d => d.CreatLa)
            .Select(d => MapeazaSpreDto(d))
            .ToListAsync();

        return Ok(rezultat);
    }

    [HttpGet("puncte/{punctId:int}/documente")]
    public async Task<IActionResult> ListaPentruPunct(int punctId)
    {
        var punct = await _context.PuncteOrdineZi.FirstOrDefaultAsync(p => p.Id == punctId);
        if (punct is null) return NotFound();
        if (!await SedintaPublica(punct.SedintaId)) return NotFound();

        var rezultat = await _context.Documente
            .Where(d => d.PunctId == punctId && d.EstePublic)
            .OrderBy(d => d.Ordine).ThenBy(d => d.CreatLa)
            .Select(d => MapeazaSpreDto(d))
            .ToListAsync();

        return Ok(rezultat);
    }

    [HttpGet("documente/{id:int}")]
    public async Task<IActionResult> Descarca(int id, CancellationToken ct)
    {
        var doc = await _context.Documente.FirstOrDefaultAsync(d => d.Id == id);
        if (doc is null || !doc.EstePublic) return NotFound();

        bool vizibil;
        if (doc.HclId != null)
        {
            vizibil = await _context.Hcluri.AnyAsync(h =>
                h.Id == doc.HclId.Value
                && h.EstePublicat
                && h.Status >= StatusActRedactional.Numerotat);
        }
        else
        {
            var sedintaId = doc.SedintaId ?? await SedintaPentruPunct(doc.PunctId!.Value);
            vizibil = sedintaId != 0 && await SedintaPublica(sedintaId);
        }

        if (!vizibil) return NotFound();

        Stream stream;
        try
        {
            stream = await _stocare.DeschideAsync(doc.CaleStocare, ct);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }

        return File(stream, doc.TipMime, doc.NumeFisierOriginal);
    }

    private Task<bool> SedintaPublica(int sedintaId)
        => _context.Sedinte.AnyAsync(s =>
            s.Id == sedintaId
            && s.Status >= StatusSedinta.Convocata
            && s.Status != StatusSedinta.Anulata);

    private async Task<int> SedintaPentruPunct(int punctId)
    {
        var p = await _context.PuncteOrdineZi
            .Where(p => p.Id == punctId)
            .Select(p => (int?)p.SedintaId)
            .FirstOrDefaultAsync();
        return p ?? 0;
    }

    private static PublicDocumentDto MapeazaSpreDto(Document d) => new(
        d.Id, d.Denumire, d.Descriere, d.TipDocument,
        d.NumeFisierOriginal, d.TipMime, d.Marime, d.Ordine);
}