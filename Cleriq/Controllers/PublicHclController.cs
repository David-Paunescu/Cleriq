using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Cleriq.Controllers;

[ApiController]
[Route("public/{slug}/hcl")]
public class PublicHclController : ControllerBase
{
    private static readonly TimeSpan TtlPdf = TimeSpan.FromHours(1);

    private readonly AppDbContext _context;
    private readonly IGeneratorPdfHcl _generatorPdf;
    private readonly IStocareDocumente _stocare;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PublicHclController> _logger;

    public PublicHclController(
        AppDbContext context,
        IGeneratorPdfHcl generatorPdf,
        IStocareDocumente stocare,
        IDistributedCache cache,
        ILogger<PublicHclController> logger)
    {
        _context = context;
        _generatorPdf = generatorPdf;
        _stocare = stocare;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Lista([FromQuery] int page = 1, [FromQuery] int size = 50)
    {
        if (page < 1) page = 1;
        if (size <= 0 || size > 200) size = 50;

        var hcluri = await _context.Hcluri
            .Where(h => h.EstePublicat && h.Status >= StatusActRedactional.Numerotat)
            .OrderByDescending(h => h.DataAdoptare)
            .ThenByDescending(h => h.Id)
            .Skip((page - 1) * size).Take(size)
            .Select(h => new PublicHclDto(
                h.Id, h.Numar, h.AnNumerotare, h.TipHcl, h.Titlu,
                h.DataAdoptare, h.DataIntrareInVigoare, h.Status,
                h.DataInvalidare != null, h.DataPublicareMol))
            .ToListAsync();

        return Ok(hcluri);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalii(int id)
    {
        var hcl = await _context.Hcluri
            .Include(h => h.Semnatari).ThenInclude(s => s.Persoana)
            .Include(h => h.Semnatari).ThenInclude(s => s.Consilier)
            .Include(h => h.Documente)
            .Include(h => h.RelatiiSursa).ThenInclude(r => r.HclTinta)
            .Include(h => h.RelatiiTinta).ThenInclude(r => r.HclSursa)
            .Where(h => h.Id == id
                     && h.EstePublicat
                     && h.Status >= StatusActRedactional.Numerotat)
            .FirstOrDefaultAsync();

        if (hcl is null) return NotFound();
        return Ok(MapeazaSpreDetaliiDto(hcl));
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> ObtinePdf(int id, CancellationToken ct)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h =>
            h.Id == id
            && h.EstePublicat
            && h.Status >= StatusActRedactional.Numerotat, ct);
        if (hcl is null) return NotFound();

        var institutie = await _context.Institutii.FirstOrDefaultAsync(ct);
        if (institutie is null) return NotFound();

        var numarNume = hcl.Numar != null && hcl.AnNumerotare != null
            ? $"{hcl.Numar}-{hcl.AnNumerotare}"
            : $"draft-{hcl.Id}";

        if (!string.IsNullOrEmpty(hcl.CaleStocareSemnat))
        {
            try
            {
                var streamSemnat = await _stocare.DeschideAsync(hcl.CaleStocareSemnat, ct);
                return File(streamSemnat, "application/pdf", $"hcl-{numarNume}-semnat.pdf");
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning(
                    "HCL semnat lipsă pe disk pentru hcl {HclId} (cheie={Cheie}). Servesc PDF generat.",
                    id, hcl.CaleStocareSemnat);
            }
        }

        byte[] pdf;
        if (hcl.Status == StatusActRedactional.Semnat)
        {
            var cheie = hcl.DataInvalidare.HasValue ? $"hcl:pdf:{id}:inv" : $"hcl:pdf:{id}";
            var dinCache = await CitestePdfDinCacheAsync(cheie, ct);
            if (dinCache is null)
            {
                dinCache = _generatorPdf.Genereaza(hcl, institutie);
                await ScriePdfInCacheAsync(cheie, dinCache, ct);
            }
            pdf = dinCache;
        }
        else
        {
            pdf = _generatorPdf.Genereaza(hcl, institutie);
        }

        return File(pdf, "application/pdf", $"hcl-{numarNume}.pdf");
    }

    private static PublicHclDetaliiDto MapeazaSpreDetaliiDto(Hcl h) => new(
        h.Id, h.Numar, h.AnNumerotare, h.TipHcl, h.Titlu, h.Continut,
        h.DataAdoptare, h.DataIntrareInVigoare, h.Status,
        h.VotPentru, h.VotImpotriva, h.VotAbtinere, h.TipMajoritate,
        !string.IsNullOrEmpty(h.CaleStocareSemnat),
        h.DataPublicareMol,
        h.MotivLipsaSemnaturaPresedinte,
        h.MotivInvalidare, h.RefInvalidare, h.DataInvalidare,
        h.Semnatari.OrderBy(s => s.OrdineAfisare).Select(MapeazaSemnatar).ToList(),
        h.Documente.Where(d => d.EstePublic).OrderBy(d => d.Ordine).Select(MapeazaAnexa).ToList(),
        h.RelatiiSursa.Select(MapeazaRelatieSursa).ToList(),
        h.RelatiiTinta.Select(MapeazaRelatieTinta).ToList());

    private static PublicSemnatarHclDto MapeazaSemnatar(SemnatarHcl s) => new(
        s.RolSemnatar,
        s.Persoana?.NumeComplet ?? s.Consilier?.NumeComplet ?? "—",
        s.OrdineAfisare);

    private static PublicDocumentHclDto MapeazaAnexa(Document d) => new(
        d.Id, d.Denumire, d.Descriere, d.TipDocumentHcl, d.NumarOrdinAnexa,
        d.NumeFisierOriginal, d.Marime, d.Ordine);

    private static PublicRelatieHclDto MapeazaRelatieSursa(RelatieHcl r)
    {
        if (r.HclTinta != null)
            return MapeazaCapatIntern(r.TipRelatie, r.HclTinta);
        return new PublicRelatieHclDto(
            r.TipRelatie, null, null, null, r.ReferintaActExternText, false);
    }

    private static PublicRelatieHclDto MapeazaRelatieTinta(RelatieHcl r)
        => MapeazaCapatIntern(r.TipRelatie, r.HclSursa);

    private static PublicRelatieHclDto MapeazaCapatIntern(TipRelatieHcl tip, Hcl celalalt)
    {
        if (celalalt.Status >= StatusActRedactional.Numerotat)
        {
            var numar = celalalt.Numar != null && celalalt.AnNumerotare != null
                ? $"{celalalt.Numar}/{celalalt.AnNumerotare}"
                : null;
            return new PublicRelatieHclDto(tip, celalalt.Id, numar, celalalt.Titlu, null, false);
        }
        return new PublicRelatieHclDto(tip, null, null, null, null, true);
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
                "Redis indisponibil la citirea PDF HCL din cache ({Cheie}); generez direct.", cheie);
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
                "Redis indisponibil la scrierea PDF HCL în cache ({Cheie}); cache sărit.", cheie);
        }
    }
}