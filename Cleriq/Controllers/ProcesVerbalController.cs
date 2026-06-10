using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Helpers;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/Sedinte/{sedintaId}/ProcesVerbal")]
[Authorize]
public class ProcesVerbalController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IGeneratorPdfProcesVerbal _generatorPdf;

    public ProcesVerbalController(
        AppDbContext context,
        IConfiguration config,
        IGeneratorPdfProcesVerbal generatorPdf)
    {
        _context = context;
        _config = config;
        _generatorPdf = generatorPdf;
    }

    [HttpGet]
    public async Task<IActionResult> Obtine(int sedintaId)
    {
        var sedintaExista = await _context.Sedinte.AnyAsync(s => s.Id == sedintaId);
        if (!sedintaExista)
            return NotFound("Ședința nu există.");

        var pv = await _context.ProceseVerbale.FirstOrDefaultAsync(p => p.SedintaId == sedintaId);
        if (pv is null)
            return NotFound("Nu există proces verbal pentru această ședință.");

        return Ok(MapeazaSpreDto(pv));
    }

    [HttpGet("Markdown")]
    public async Task<IActionResult> ObtineMarkdown(int sedintaId)
    {
        var pv = await _context.ProceseVerbale.FirstOrDefaultAsync(p => p.SedintaId == sedintaId);
        if (pv is null)
            return NotFound();
        return Content(pv.Continut ?? "", "text/markdown; charset=utf-8");
    }

    // PDF generat on-the-fly din Continut (Markdown). Draft → watermark DRAFT.
    // Accesibil pe orice status — secretarul vrea preview și pe Draft.
    [HttpGet("Pdf")]
    public async Task<IActionResult> ObtinePdf(int sedintaId)
    {
        var sedinta = await _context.Sedinte
            .Include(s => s.Institutie)
            .FirstOrDefaultAsync(s => s.Id == sedintaId);
        if (sedinta is null)
            return NotFound("Ședința nu există.");

        var pv = await _context.ProceseVerbale
            .FirstOrDefaultAsync(p => p.SedintaId == sedintaId);
        if (pv is null)
            return NotFound("Nu există proces verbal pentru această ședință.");

        var pdf = _generatorPdf.Genereaza(pv, sedinta.Institutie);

        var dataLocala = sedinta.DataOra.LaFusOrar(sedinta.Institutie.FusOrar);
        return File(pdf, "application/pdf", $"proces-verbal-{dataLocala:yyyy-MM-dd}.pdf");
    }

    [HttpPost("Genereaza")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Genereaza(int sedintaId)
    {
        var sedinta = await _context.Sedinte
            .Include(s => s.Institutie)
            .Include(s => s.Documente)
            .Include(s => s.Puncte.OrderBy(p => p.Ordine))
                .ThenInclude(p => p.Documente)
            .Include(s => s.Puncte.OrderBy(p => p.Ordine))
                .ThenInclude(p => p.Voturi)
                    .ThenInclude(v => v.Consilier)
            .Include(s => s.Prezente)
                .ThenInclude(p => p.Consilier)
            .FirstOrDefaultAsync(s => s.Id == sedintaId);

        if (sedinta is null)
            return NotFound("Ședința nu există.");

        var pv = await _context.ProceseVerbale.FirstOrDefaultAsync(p => p.SedintaId == sedintaId);

        if (pv is not null && pv.Status == StatusProcesVerbal.Finalizat)
            return Conflict("Procesul verbal este finalizat și nu mai poate fi regenerat.");

        var consilieriActivi = await _context.Consilieri
            .Where(c => c.Activ)
            .OrderBy(c => c.NumeComplet)
            .ToListAsync();

        var urlBaza = _config["Portal:UrlBaza"]?.TrimEnd('/') ?? "";
        var continut = GenereazaContinut(sedinta, consilieriActivi, urlBaza);
        var acum = DateTime.UtcNow;

        if (pv is null)
        {
            pv = new ProcesVerbal
            {
                SedintaId = sedintaId,
                Continut = continut,
                Status = StatusProcesVerbal.Draft,
                DataGenerare = acum
            };
            _context.ProceseVerbale.Add(pv);
        }
        else
        {
            pv.Continut = continut;
            pv.DataGenerare = acum;
        }

        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(pv));
    }

    [HttpPut]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Editeaza(int sedintaId, EditareProcesVerbalDto dto)
    {
        var pv = await _context.ProceseVerbale.FirstOrDefaultAsync(p => p.SedintaId == sedintaId);
        if (pv is null)
            return NotFound("Nu există proces verbal. Generează-l mai întâi.");

        if (pv.Status == StatusProcesVerbal.Finalizat)
            return Conflict("Procesul verbal este finalizat. Nu mai poate fi editat.");

        pv.Continut = dto.Continut;
        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(pv));
    }

    [HttpPost("Finalizeaza")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Finalizeaza(int sedintaId)
    {
        var pv = await _context.ProceseVerbale.FirstOrDefaultAsync(p => p.SedintaId == sedintaId);
        if (pv is null)
            return NotFound("Nu există proces verbal pentru această ședință.");

        if (pv.Status == StatusProcesVerbal.Finalizat)
            return Conflict("Procesul verbal este deja finalizat.");

        pv.Status = StatusProcesVerbal.Finalizat;
        pv.DataFinalizare = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(pv));
    }

    // ============= Generator Markdown =============

    private static string GenereazaContinut(Sedinta s, List<Consilier> consilieriActivi, string urlBaza)
    {
        var sb = new StringBuilder();
        var culturaRo = new CultureInfo("ro-RO");

        sb.AppendLine($"# Proces verbal — {s.Titlu}");
        sb.AppendLine();
        sb.AppendLine($"**Instituția:** {s.Institutie.Denumire}");
        if (!string.IsNullOrWhiteSpace(s.Numar))
            sb.AppendLine($"**Număr ședință:** {s.Numar}");
        var dataOraLocala = s.DataOra.LaFusOrar(s.Institutie.FusOrar);
        var indicatorFus = s.DataOra.IndicatorFusOrar(s.Institutie.FusOrar);
        var dataOraText = dataOraLocala.ToString("dd MMMM yyyy, HH:mm", culturaRo);
        sb.AppendLine($"**Data și ora:** {(string.IsNullOrEmpty(indicatorFus) ? dataOraText : $"{dataOraText} ({indicatorFus})")}");
        if (!string.IsNullOrWhiteSpace(s.Loc))
            sb.AppendLine($"**Loc:** {s.Loc}");
        sb.AppendLine($"**Tip ședință:** {s.Tip.Eticheta()}");
        sb.AppendLine($"**Mod desfășurare:** {s.ModDesfasurare.Eticheta()}");
        sb.AppendLine();

        // Prezență
        sb.AppendLine("## Prezență");
        sb.AppendLine();
        var prezenteDict = s.Prezente.ToDictionary(p => p.ConsilierId);
        var totalActivi = consilieriActivi.Count;
        var prezenti = 0;
        foreach (var c in consilieriActivi)
        {
            var status = prezenteDict.TryGetValue(c.Id, out var p) ? p.Status : StatusPrezenta.Absent;
            if (status == StatusPrezenta.Prezent || status == StatusPrezenta.OnlinePrezent)
                prezenti++;
            sb.AppendLine($"- {c.NumeComplet}: {status.Eticheta()}");
        }
        sb.AppendLine();
        var cvorumNecesar = (totalActivi / 2) + 1;
        sb.AppendLine($"**Total consilieri în funcție:** {totalActivi}");
        sb.AppendLine($"**Prezenți:** {prezenti}");
        sb.AppendLine($"**Cvorum necesar:** {cvorumNecesar}");
        sb.AppendLine($"**Cvorum:** {(prezenti >= cvorumNecesar ? "întrunit" : "neîntrunit")}");
        sb.AppendLine();

        // Ordine de zi
        sb.AppendLine("## Ordine de zi");
        sb.AppendLine();

        if (!s.Puncte.Any())
        {
            sb.AppendLine("_(Nicio ordine de zi)_");
            sb.AppendLine();
            return sb.ToString();
        }

        var docSedinta = s.Documente
            .Where(d => d.EstePublic)
            .OrderBy(d => d.Ordine).ThenBy(d => d.CreatLa)
            .ToList();

                if (docSedinta.Any())
                {
                    sb.AppendLine("## Documente atașate ședinței");
                    sb.AppendLine();
                    foreach (var d in docSedinta)
                    {
                        sb.AppendLine($"- [{d.Denumire}]({urlBaza}/public/{s.Institutie.Slug}/documente/{d.Id}) ({d.TipDocument.Eticheta()})");
                    }
                    sb.AppendLine();
                }

        foreach (var punct in s.Puncte)
        {
            sb.AppendLine($"### {punct.Ordine}. {punct.Titlu}");
            sb.AppendLine();
            sb.AppendLine($"**Tip:** {punct.Tip.Eticheta()}");
            if (!string.IsNullOrWhiteSpace(punct.Descriere))
                sb.AppendLine($"**Descriere:** {punct.Descriere}");


            if (!punct.NecesitaVot)
            {
                sb.AppendLine("**Notă:** Punct fără vot (informare).");
                sb.AppendLine();
                continue;
            }

            sb.AppendLine($"**Tip majoritate:** {punct.TipMajoritate.Eticheta()}");
            sb.AppendLine($"**Rezultat:** {punct.Rezultat.Eticheta()}");
            sb.AppendLine();

            var docPunct = punct.Documente
                .Where(d => d.EstePublic)
                .OrderBy(d => d.Ordine).ThenBy(d => d.CreatLa)
                .ToList();

                        if (docPunct.Any())
                        {
                            sb.AppendLine();
                            sb.AppendLine("**Documente:**");
                            foreach (var d in docPunct)
                            {
                                sb.AppendLine($"- [{d.Denumire}]({urlBaza}/public/{s.Institutie.Slug}/documente/{d.Id}) ({d.TipDocument.Eticheta()})");
                            }
                        }

            var rezumat = punct.Rezumat(punct.Voturi);

            if (rezumat.Secret)
            {
                sb.AppendLine("**Vot secret.** Conform legii, voturile individuale nu se consemnează nominal.");
                sb.AppendLine();
                sb.AppendLine($"- Pentru: {rezumat.Pentru}");
                sb.AppendLine($"- Împotrivă: {rezumat.Impotriva}");
                sb.AppendLine($"- Abțineri: {rezumat.Abtineri}");
                sb.AppendLine($"- Total voturi exprimate: {rezumat.TotalExprimate}");
                sb.AppendLine();
            }
            else
            {
                var pentru = rezumat.VoturiNominale.Where(v => v.Optiune == OptiuneVot.Pentru)
                    .Select(v => v.Consilier.NumeComplet).ToList();
                var impotriva = rezumat.VoturiNominale.Where(v => v.Optiune == OptiuneVot.Impotriva)
                    .Select(v => v.Consilier.NumeComplet).ToList();
                var abtinere = rezumat.VoturiNominale.Where(v => v.Optiune == OptiuneVot.Abtinere)
                    .Select(v => v.Consilier.NumeComplet).ToList();

                sb.AppendLine("**Voturi nominale:**");
                sb.AppendLine($"- Pentru ({pentru.Count}): {(pentru.Any() ? string.Join(", ", pentru) : "—")}");
                sb.AppendLine($"- Împotrivă ({impotriva.Count}): {(impotriva.Any() ? string.Join(", ", impotriva) : "—")}");
                sb.AppendLine($"- Abțineri ({abtinere.Count}): {(abtinere.Any() ? string.Join(", ", abtinere) : "—")}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static ProcesVerbalDto MapeazaSpreDto(ProcesVerbal pv) => new(
        pv.Id, pv.SedintaId, pv.Continut, pv.Status,
        pv.DataGenerare, pv.DataFinalizare, pv.InstitutieId, pv.CreatLa);
}