using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Helpers;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Cleriq.Controllers;

[ApiController]
[Route("public/{slug}/sedinte/{sedintaId:int}/procesverbal")]
public class PublicProcesVerbalController : ControllerBase
{
    // PDF-ul unui PV Finalizat e imutabil (conținut înghețat) → cache sigur.
    // TTL-ul limitează doar staleness-ul pe metadatele instituției (denumire — schimbări rare).
    private static readonly TimeSpan TtlPdf = TimeSpan.FromHours(1);

    private readonly AppDbContext _context;
    private readonly IGeneratorPdfProcesVerbal _generatorPdf;
    private readonly IStocareDocumente _stocare;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PublicProcesVerbalController> _logger;

    public PublicProcesVerbalController(
        AppDbContext context,
        IGeneratorPdfProcesVerbal generatorPdf,
        IStocareDocumente stocare,
        IDistributedCache cache,
        ILogger<PublicProcesVerbalController> logger)
    {
        _context = context;
        _generatorPdf = generatorPdf;
        _stocare = stocare;
        _cache = cache;
        _logger = logger;
    }

    // GET /public/{slug}/sedinte/{sedintaId}/procesverbal
    [HttpGet]
    public async Task<IActionResult> Obtine(int sedintaId)
    {
        var pv = await ObtinePvPublic(sedintaId);
        if (pv is null) return NotFound();

        return Ok(new PublicProcesVerbalDto(
            pv.SedintaId, pv.Continut, pv.DataFinalizare,
            !string.IsNullOrEmpty(pv.CaleStocareSemnat)));
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
    // Varianta SEMNATĂ (act oficial) are prioritate. Nume canonic, NU numele
    // fișierului încărcat de secretar (nu scurgem denumiri interne).
    // Dacă fișierul semnat lipsește de pe disk (integritate ruptă): warning în log
    // + degradare grațioasă la PDF-ul generat — cetățeanul primește mereu un PDF.
    [HttpGet("pdf")]
    public async Task<IActionResult> ObtinePdf(int sedintaId, CancellationToken ct)
    {
        var pv = await ObtinePvPublic(sedintaId);
        if (pv is null) return NotFound();

        var institutie = await _context.Institutii.FirstOrDefaultAsync(ct);
        if (institutie is null) return NotFound();

        // Ședința există garantat (verificat în ObtinePvPublic) — luăm doar data.
        var dataOra = await _context.Sedinte
            .Where(s => s.Id == sedintaId)
            .Select(s => s.DataOra)
            .FirstAsync(ct);
        var dataLocala = dataOra.LaFusOrar(institutie.FusOrar);

        if (!string.IsNullOrEmpty(pv.CaleStocareSemnat))
        {
            try
            {
                var stream = await _stocare.DeschideAsync(pv.CaleStocareSemnat, ct);
                return File(stream, "application/pdf",
                    $"proces-verbal-{dataLocala:yyyy-MM-dd}-semnat.pdf");
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning(
                    "PV semnat lipsă pe disk pentru sedinta {SedintaId} (cheie={Cheie}). Servesc PDF generat.",
                    sedintaId, pv.CaleStocareSemnat);
            }
        }

        // Generarea costă CPU (~50-200ms) pe un endpoint neautentificat → cache Redis.
        // Cache-uim DOAR generarea; verificările de vizibilitate de mai sus rămân
        // per-request — o ședință ascunsă/ștearsă nu mai servește PDF imediat.
        var cheie = $"pv:pdf:{sedintaId}";
        var pdf = await CitestePdfDinCacheAsync(cheie, ct);
        if (pdf is null)
        {
            pdf = _generatorPdf.Genereaza(pv, institutie);
            await ScriePdfInCacheAsync(cheie, pdf, ct);
        }

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

    private async Task<byte[]?> CitestePdfDinCacheAsync(string cheie, CancellationToken ct)
    {
        try
        {
            return await _cache.GetAsync(cheie, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "Redis indisponibil la citirea PDF din cache ({Cheie}); generez direct.", cheie);
            return null;
        }
    }

    private async Task ScriePdfInCacheAsync(string cheie, byte[] pdf, CancellationToken ct)
    {
        try
        {
            await _cache.SetAsync(cheie, pdf,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TtlPdf },
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "Redis indisponibil la scrierea PDF în cache ({Cheie}); cache sărit.", cheie);
        }
    }
}