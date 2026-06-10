using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cleriq.Helpers;
using Cleriq.Services;

namespace Cleriq.Controllers;

[ApiController]
[Route("public/{slug}/sedinte/{sedintaId:int}/procesverbal")]
public class PublicProcesVerbalController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IGeneratorPdfProcesVerbal _generatorPdf;

    public PublicProcesVerbalController(
        AppDbContext context,
        IGeneratorPdfProcesVerbal generatorPdf)
    {
        _context = context;
        _generatorPdf = generatorPdf;
    }

    // GET /public/{slug}/sedinte/{sedintaId}/procesverbal
    [HttpGet]
    public async Task<IActionResult> Obtine(int sedintaId)
    {
        var pv = await ObtinePvPublic(sedintaId);
        if (pv is null) return NotFound();

        return Ok(new PublicProcesVerbalDto(
            pv.SedintaId, pv.Continut, pv.DataFinalizare));
    }

    // GET /public/{slug}/sedinte/{sedintaId}/procesverbal/markdown
    [HttpGet("markdown")]
    public async Task<IActionResult> ObtineMarkdown(int sedintaId)
    {
        var pv = await ObtinePvPublic(sedintaId);
        if (pv is null) return NotFound();

        return Content(pv.Continut ?? "", "text/markdown; charset=utf-8");
    }

    // GET /public/{slug}/sedinte/{sedintaId}/procesverbal/pdf
    // Aceleași reguli de vizibilitate ca JSON/Markdown: doar PV Finalizat + ședință publicabilă.
    // Notă viitor (Nivel 1 semnătură): aici se va servi PDF-ul semnat dacă există.
    [HttpGet("pdf")]
    public async Task<IActionResult> ObtinePdf(int sedintaId)
    {
        var pv = await ObtinePvPublic(sedintaId);
        if (pv is null) return NotFound();

        var institutie = await _context.Institutii.FirstOrDefaultAsync();
        if (institutie is null) return NotFound();

        // Ședința există garantat (verificat în ObtinePvPublic) — luăm doar data.
        var dataOra = await _context.Sedinte
            .Where(s => s.Id == sedintaId)
            .Select(s => s.DataOra)
            .FirstAsync();

        var pdf = _generatorPdf.Genereaza(pv, institutie);

        var dataLocala = dataOra.LaFusOrar(institutie.FusOrar);
        return File(pdf, "application/pdf", $"proces-verbal-{dataLocala:yyyy-MM-dd}.pdf");
    }

    // PV public dacă: ședința e Convocata/InDesfasurare/Finalizata (NU Planificata/Anulata)
    //              și PV are Status=Finalizat
    private async Task<ProcesVerbal?> ObtinePvPublic(int sedintaId)
    {
        var sedintaPublica = await _context.Sedinte.AnyAsync(s =>
            s.Id == sedintaId
            && s.Status >= StatusSedinta.Convocata
            && s.Status != StatusSedinta.Anulata);
        if (!sedintaPublica) return null;

        return await _context.ProceseVerbale.FirstOrDefaultAsync(p =>
            p.SedintaId == sedintaId
            && p.Status == StatusProcesVerbal.Finalizat);
    }
}