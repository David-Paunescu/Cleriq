using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class MentenantaController : ControllerBase
{
    // Gardă universală anti-race: fișierele atinse mai recent de 1h sunt protejate.
    // Acoperă fereastra de ~ms între SalveazaAsync și SaveChangesAsync la upload.
    private static readonly TimeSpan VarstaMinimaFisier = TimeSpan.FromHours(1);

    private const int ZilePragMin = 30;
    private const int ZilePragDefault = 90;

    private readonly AppDbContext _context;
    private readonly IStocareDocumente _stocareDocumente;
    private readonly IStocareAudio _stocareAudio;
    private readonly ILogger<MentenantaController> _logger;
    private readonly IFurnizorUtilizator _utilizator;

    public MentenantaController(
        AppDbContext context,
        IStocareDocumente stocareDocumente,
        IStocareAudio stocareAudio,
        ILogger<MentenantaController> logger,
        IFurnizorUtilizator utilizator)
    {
        _context = context;
        _stocareDocumente = stocareDocumente;
        _stocareAudio = stocareAudio;
        _logger = logger;
        _utilizator = utilizator;
    }

    // GET = preview read-only. Identifică orfanii fără să șteargă nimic.
    [HttpGet("OrfaniDocumente")]
    public async Task<IActionResult> ListaOrfaniDocumente(
        [FromQuery] int zile = ZilePragDefault,
        CancellationToken ct = default)
    {
        if (zile < ZilePragMin)
            return BadRequest($"Pragul minim este {ZilePragMin} zile (gardă defensivă).");

        var raport = await CalculeazaOrfaniDocumenteAsync(zile, ct);
        return Ok(raport);
    }

    // POST = șterge fizic fișierele identificate ca orfani.
    [HttpPost("OrfaniDocumente")]
    public async Task<IActionResult> StergeOrfaniDocumente(
        [FromQuery] int zile = ZilePragDefault,
        CancellationToken ct = default)
    {
        if (zile < ZilePragMin)
            return BadRequest($"Pragul minim este {ZilePragMin} zile (gardă defensivă).");

        var raport = await CalculeazaOrfaniDocumenteAsync(zile, ct);

        _logger.LogInformation(
            "Cleanup documente orfane invocat de SuperAdmin {UserId}: {Total} fișiere, {Bytes} bytes, prag {Zile} zile.",
            _utilizator.UserId, raport.TotalFisiere, raport.TotalBytes, zile);

        var fisiereSterse = 0;
        long bytesEliberati = 0;
        var ramase = new List<FisierOrfanDto>();

        foreach (var orfan in raport.Fisiere)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await _stocareDocumente.StergeAsync(orfan.Cheie, ct);
                fisiereSterse++;
                bytesEliberati += orfan.Marime;

                _logger.LogInformation(
                    "Șters document orfan: cheie={Cheie}, marime={Marime}B, categorie={Categorie}.",
                    orfan.Cheie, orfan.Marime, orfan.Categorie);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Eșec ștergere fișier orfan: {Cheie}. Rămâne în raport, retry manual.",
                    orfan.Cheie);
                ramase.Add(orfan);
            }
        }

        // Raport final = fișierele care au eșuat la ștergere (rare; corupție disk, permisiuni)
        return Ok(new RezultatStergereDto(
            TotalCandidati: raport.TotalFisiere,
            Sterse: fisiereSterse,
            Esuate: ramase.Count,
            BytesEliberati: bytesEliberati,
            SterseFaraRandInDb: raport.CategoriaFaraRandInDb - ramase.Count(o => o.Categorie == CategorieOrfan.FaraRandInDb),
            SterseSoftDeletedVechi: raport.CategoriaSoftDeletedVechi - ramase.Count(o => o.Categorie == CategorieOrfan.SoftDeletedVechi),
            ZilePrag: zile,
            FisiereEsuate: ramase));
    }

    // ============ Audio ============

    [HttpGet("OrfaniAudio")]
    public async Task<IActionResult> ListaOrfaniAudio(
        [FromQuery] int zile = ZilePragDefault,
        CancellationToken ct = default)
    {
        if (zile < ZilePragMin)
            return BadRequest($"Pragul minim este {ZilePragMin} zile (gardă defensivă).");

        var raport = await CalculeazaOrfaniAudioAsync(zile, ct);
        return Ok(raport);
    }

    [HttpPost("OrfaniAudio")]
    public async Task<IActionResult> StergeOrfaniAudio(
        [FromQuery] int zile = ZilePragDefault,
        CancellationToken ct = default)
    {
        if (zile < ZilePragMin)
            return BadRequest($"Pragul minim este {ZilePragMin} zile (gardă defensivă).");

        var raport = await CalculeazaOrfaniAudioAsync(zile, ct);

        _logger.LogInformation(
            "Cleanup audio orfan invocat de SuperAdmin {UserId}: {Total} fișiere, {Bytes} bytes, prag {Zile} zile.",
            _utilizator.UserId, raport.TotalFisiere, raport.TotalBytes, zile);

        var fisiereSterse = 0;
        long bytesEliberati = 0;
        var ramase = new List<FisierOrfanDto>();

        foreach (var orfan in raport.Fisiere)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                await _stocareAudio.StergeAsync(orfan.Cheie, ct);
                fisiereSterse++;
                bytesEliberati += orfan.Marime;

                _logger.LogInformation(
                    "Șters audio orfan: cheie={Cheie}, marime={Marime}B, categorie={Categorie}.",
                    orfan.Cheie, orfan.Marime, orfan.Categorie);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Eșec ștergere audio orfan: {Cheie}. Rămâne în raport, retry manual.",
                    orfan.Cheie);
                ramase.Add(orfan);
            }
        }

        return Ok(new RezultatStergereDto(
            TotalCandidati: raport.TotalFisiere,
            Sterse: fisiereSterse,
            Esuate: ramase.Count,
            BytesEliberati: bytesEliberati,
            SterseFaraRandInDb: raport.CategoriaFaraRandInDb - ramase.Count(o => o.Categorie == CategorieOrfan.FaraRandInDb),
            SterseSoftDeletedVechi: raport.CategoriaSoftDeletedVechi - ramase.Count(o => o.Categorie == CategorieOrfan.SoftDeletedVechi),
            ZilePrag: zile,
            FisiereEsuate: ramase));
    }

    private async Task<RaportOrfaniDto> CalculeazaOrfaniAudioAsync(
        int zilePrag, CancellationToken ct)
    {
        var acum = DateTime.UtcNow;
        var pragVarstaFisier = acum - VarstaMinimaFisier;
        var pragSoftDelete = acum - TimeSpan.FromDays(zilePrag);

        // Audio = un fișier per Transcriere (one-to-one cu Sedinta, prin index unic filtrat).
        // Citim TOATE transcrierile (active + soft-deleted), peste toate instituțiile.
        var transcrieriDb = await _context.Transcrieri
            .IgnoreQueryFilters()
            .Select(t => new
            {
                t.CaleStocareAudio,
                t.EsteSters,
                t.StersLa,
                t.InstitutieId
            })
            .ToListAsync(ct);

        var dictDb = transcrieriDb.ToDictionary(t => t.CaleStocareAudio);

        var orfani = new List<FisierOrfanDto>();
        int faraRand = 0;
        int softDeletedVechi = 0;

        await foreach (var fisier in _stocareAudio.EnumereazaToateAsync(ct))
        {
            if (fisier.DataModificare > pragVarstaFisier)
                continue;

            if (!dictDb.TryGetValue(fisier.Cheie, out var dbEntry))
            {
                orfani.Add(new FisierOrfanDto(
                    fisier.Cheie, fisier.Marime, fisier.DataModificare,
                    CategorieOrfan.FaraRandInDb, null, null));
                faraRand++;
            }
            else if (dbEntry.EsteSters
                  && dbEntry.StersLa.HasValue
                  && dbEntry.StersLa.Value < pragSoftDelete)
            {
                orfani.Add(new FisierOrfanDto(
                    fisier.Cheie, fisier.Marime, fisier.DataModificare,
                    CategorieOrfan.SoftDeletedVechi,
                    dbEntry.StersLa, dbEntry.InstitutieId));
                softDeletedVechi++;
            }
        }

        return new RaportOrfaniDto(
            TotalFisiere: orfani.Count,
            TotalBytes: orfani.Sum(o => o.Marime),
            CategoriaFaraRandInDb: faraRand,
            CategoriaSoftDeletedVechi: softDeletedVechi,
            ZilePrag: zilePrag,
            Fisiere: orfani);
    }

    // ============ Helper privat ============

    private async Task<RaportOrfaniDto> CalculeazaOrfaniDocumenteAsync(
        int zilePrag, CancellationToken ct)
    {
        var acum = DateTime.UtcNow;
        var pragVarstaFisier = acum - VarstaMinimaFisier;
        var pragSoftDelete = acum - TimeSpan.FromDays(zilePrag);

        // 1. Snapshot DB: toate documentele (active + soft-deleted), cu CaleStocare, StersLa, InstitutieId
        var documenteDb = await _context.Documente
            .IgnoreQueryFilters()
            .Select(d => new
            {
                d.CaleStocare,
                d.EsteSters,
                d.StersLa,
                d.InstitutieId
            })
            .ToListAsync(ct);

        // PV/HCL/Dispoziții semnate trăiesc în ACEEAȘI stocare fizică (IStocareDocumente), dar sunt
        // referențiate din CaleStocareSemnat-ul fiecăruia, nu din Documente. Fără includerea lor
        // aici, scanerul le-ar clasifica FaraRandInDb și le-ar ȘTERGE (la dispoziții = PDF-uri de
        // personal semnate → impact pe probe + arhivare).
        var pvSemnateDb = await _context.ProceseVerbale
                    .IgnoreQueryFilters()
                    .Where(p => p.CaleStocareSemnat != null)
                    .Select(p => new
                    {
                        CaleStocare = p.CaleStocareSemnat!,
                        p.EsteSters,
                        p.StersLa,
                        p.InstitutieId
                    })
                    .ToListAsync(ct);

        var hclSemnateDb = await _context.Hcluri
            .IgnoreQueryFilters()
            .Where(h => h.CaleStocareSemnat != null)
            .Select(h => new
            {
                CaleStocare = h.CaleStocareSemnat!,
                h.EsteSters,
                h.StersLa,
                h.InstitutieId
            })
            .ToListAsync(ct);

        var dispozitiiSemnateDb = await _context.Dispozitii
            .IgnoreQueryFilters()
            .Where(d => d.CaleStocareSemnat != null)
            .Select(d => new
            {
                CaleStocare = d.CaleStocareSemnat!,
                d.EsteSters,
                d.StersLa,
                d.InstitutieId
            })
            .ToListAsync(ct);

        var dictDb = documenteDb
            .Concat(pvSemnateDb)
            .Concat(hclSemnateDb)
            .Concat(dispozitiiSemnateDb)
            .ToDictionary(d => d.CaleStocare);

        // 2. Enumerăm fișierele fizice și clasificăm
        var orfani = new List<FisierOrfanDto>();
        int faraRand = 0;
        int softDeletedVechi = 0;

        await foreach (var fisier in _stocareDocumente.EnumereazaToateAsync(ct))
        {
            // Gardă universală: prea recent atins → SKIP, indiferent de categorie
            if (fisier.DataModificare > pragVarstaFisier)
                continue;

            if (!dictDb.TryGetValue(fisier.Cheie, out var dbEntry))
            {
                // Categoria 1: fișier pe disk, fără rând în DB
                orfani.Add(new FisierOrfanDto(
                    fisier.Cheie, fisier.Marime, fisier.DataModificare,
                    CategorieOrfan.FaraRandInDb, null, null));
                faraRand++;
            }
            else if (dbEntry.EsteSters
                  && dbEntry.StersLa.HasValue
                  && dbEntry.StersLa.Value < pragSoftDelete)
            {
                // Categoria 2: soft-deleted suficient de vechi
                orfani.Add(new FisierOrfanDto(
                    fisier.Cheie, fisier.Marime, fisier.DataModificare,
                    CategorieOrfan.SoftDeletedVechi,
                    dbEntry.StersLa, dbEntry.InstitutieId));
                softDeletedVechi++;
            }
            // Altfel: rând activ în DB sau soft-deleted recent → NU e orfan
        }

        return new RaportOrfaniDto(
            TotalFisiere: orfani.Count,
            TotalBytes: orfani.Sum(o => o.Marime),
            CategoriaFaraRandInDb: faraRand,
            CategoriaSoftDeletedVechi: softDeletedVechi,
            ZilePrag: zilePrag,
            Fisiere: orfani);
    }
}