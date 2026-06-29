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
public class HclController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IServiciuFunctiiIstorice _functiiIstorice;
    private readonly IServiciuNumerotareHcl _numerotare;
    private readonly IGeneratorHcl _generator;
    private readonly IGeneratorPdfHcl _generatorPdf;
    private readonly IStocareDocumente _stocare;

    public HclController(
        AppDbContext context,
        IServiciuFunctiiIstorice functiiIstorice,
        IServiciuNumerotareHcl numerotare,
        IGeneratorHcl generator,
        IGeneratorPdfHcl generatorPdf,
        IStocareDocumente stocare)
    {
        _context = context;
        _functiiIstorice = functiiIstorice;
        _numerotare = numerotare;
        _generator = generator;
        _generatorPdf = generatorPdf;
        _stocare = stocare;
    }

    [HttpGet]
    public async Task<IActionResult> Lista(
        [FromQuery] int? an,
        [FromQuery] StatusHclRedactional? status,
        [FromQuery] TipHcl? tipHcl,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        if (take <= 0 || take > 200) take = 50;
        if (skip < 0) skip = 0;

        var query = _context.Hcluri.AsQueryable();
        if (an.HasValue) query = query.Where(h => h.AnNumerotare == an.Value);
        if (status.HasValue) query = query.Where(h => h.Status == status.Value);
        if (tipHcl.HasValue) query = query.Where(h => h.TipHcl == tipHcl.Value);

        var hcluri = await query
            .OrderByDescending(h => h.DataAdoptare)
            .ThenByDescending(h => h.Id)
            .Skip(skip).Take(take)
            .ToListAsync();

        return Ok(hcluri.Select(MapeazaSpreDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalii(int id)
    {
        var hcl = await _context.Hcluri.CuIncludeComplet().FirstOrDefaultAsync(h => h.Id == id);
        if (hcl is null) return NotFound();
        return Ok(MapareHcl.SpreDetaliiDto(hcl));
    }

    [HttpGet("{id}/Pdf")]
    public async Task<IActionResult> ObtinePdf(int id)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == id);
        if (hcl is null) return NotFound();

        var institutie = await _context.Institutii.FirstOrDefaultAsync();
        if (institutie is null) return NotFound();

        var pdf = _generatorPdf.Genereaza(hcl, institutie);
        var nume = hcl.Numar != null
            ? $"hcl-{hcl.Numar}-{hcl.AnNumerotare}.pdf"
            : $"hcl-draft-{hcl.Id}.pdf";

        return File(pdf, "application/pdf", nume);
    }

    [HttpPost("{id}/Semnat")]
    [Authorize(Roles = "Admin,Secretar")]
    [RequestSizeLimit(ValidareDocument.MarimeMaxima)]
    public async Task<IActionResult> IncarcaSemnat(
    int id, [FromForm] IFormFile fisier, CancellationToken ct)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (hcl is null) return NotFound();

        if (hcl.Status != StatusHclRedactional.Semnat)
            return Conflict("Doar un HCL semnat poate primi varianta PDF semnată.");

        if (hcl.CaleStocareSemnat != null && hcl.DataPublicareMol != null)
            return Conflict("HCL publicat în MOL — varianta semnată nu mai poate fi înlocuită.");

        if (fisier is null || fisier.Length == 0)
            return BadRequest("Fișier lipsă.");
        if (fisier.Length > ValidareDocument.MarimeMaxima)
            return BadRequest($"Fișierul depășește limita de {ValidareDocument.MarimeMaxima / (1024 * 1024)} MB.");

        var extensie = Path.GetExtension(fisier.FileName);
        if (!string.Equals(extensie, ".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Doar fișiere PDF sunt acceptate pentru HCL semnat.");

        var caleVeche = hcl.CaleStocareSemnat;

        FisierStocat stocat;
        await using (var stream = fisier.OpenReadStream())
        {
            stocat = await _stocare.SalveazaAsync(
                _context.InstitutieIdCurenta, fisier.FileName, stream, ct);
        }

        hcl.CaleStocareSemnat = stocat.Cheie;
        hcl.NumeFisierSemnat = Path.GetFileName(fisier.FileName);
        hcl.MarimeSemnat = stocat.Marime;
        hcl.HashSha256Semnat = stocat.HashSha256;
        hcl.DataIncarcareSemnat = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(caleVeche) && caleVeche != stocat.Cheie)
        {
            try { await _stocare.StergeAsync(caleVeche, ct); }
            catch { }
        }

        var hclComplet = await ReincarcaCuIncludeAsync(id, ct);
        return Ok(MapareHcl.SpreDetaliiDto(hclComplet));
    }

    [HttpGet("{id}/Semnat")]
    public async Task<IActionResult> DescarcaSemnat(int id, CancellationToken ct)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (hcl is null || string.IsNullOrEmpty(hcl.CaleStocareSemnat))
            return NotFound("Nu există variantă semnată încărcată.");

        Stream stream;
        try
        {
            stream = await _stocare.DeschideAsync(hcl.CaleStocareSemnat, ct);
        }
        catch (FileNotFoundException)
        {
            return NotFound("Fișierul fizic lipsește.");
        }

        return File(stream, "application/pdf",
            hcl.NumeFisierSemnat ?? "hcl-semnat.pdf");
    }

    [HttpDelete("{id}/Semnat")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> StergeSemnat(int id, CancellationToken ct)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (hcl is null || string.IsNullOrEmpty(hcl.CaleStocareSemnat))
            return NotFound("Nu există variantă semnată încărcată.");

        if (hcl.DataPublicareMol != null)
            return Conflict("HCL publicat în MOL — varianta semnată nu mai poate fi ștearsă.");

        hcl.CaleStocareSemnat = null;
        hcl.NumeFisierSemnat = null;
        hcl.MarimeSemnat = null;
        hcl.HashSha256Semnat = null;
        hcl.DataIncarcareSemnat = null;

        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("Genereaza")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Genereaza(CreareHclDto dto)
    {
        var punct = await _context.PuncteOrdineZi
            .Include(p => p.Sedinta).ThenInclude(s => s.Institutie)
            .Include(p => p.Voturi)
            .FirstOrDefaultAsync(p => p.Id == dto.PunctOrdineZiId);

        // Precondiție 1 — punct valid + adoptat
        if (punct is null)
            return BadRequest("Punctul de ordine de zi nu există.");
        if (punct.Tip != TipPunct.ProiectHCL)
            return BadRequest("Doar punctele de tip Proiect HCL pot genera o hotărâre.");
        if (punct.Rezultat != RezultatPunct.Adoptat)
            return BadRequest("HCL se generează doar din puncte cu rezultat Adoptat.");
        if (punct.TipMajoritate is null)
            return BadRequest("Punctul adoptat nu are tip de majoritate setat.");

        var sedinta = punct.Sedinta;
        var institutie = sedinta.Institutie;

        // Precondiție 2 — ședință cel puțin convocată și NU anulată (fix #3: Anulata=5 > Convocata=2)
        if (sedinta.Status < StatusSedinta.Convocata || sedinta.Status == StatusSedinta.Anulata)
            return BadRequest("Ședința trebuie să fie cel puțin convocată și să nu fie anulată.");

        // Precondiție 3 — președinte de ședință setat
        if (sedinta.PresedinteSedintaConsilierId is null)
            return BadRequest("Înainte de a genera HCL, marchează președintele de ședință prin POST /api/Sedinte/{id}/PresedinteSedinta.");

        // Precondiție 4 — secretar UAT valid la data adoptării (fix #1: data LOCALĂ)
        var dataAdoptareLocala = DateOnly.FromDateTime(sedinta.DataOra.LaFusOrar(institutie.FusOrar));
        var secretarUat = await _functiiIstorice.CinESecretarulUatLa(dataAdoptareLocala);
        if (secretarUat is null)
            return BadRequest("Nu există Secretar UAT valid la data adoptării. Verifică Funcții oficiale și adaugă mandatul corespunzător.");

        // Precondiție 5 — un singur HCL per punct (409, nu 400)
        var existaHcl = await _context.Hcluri.AnyAsync(h => h.PunctOrdineZiId == dto.PunctOrdineZiId);
        if (existaHcl)
            return Conflict("Există deja un HCL generat din acest punct de ordine de zi.");

        // Vot snapshot
        var pentru = punct.Voturi.Count(v => v.Optiune == OptiuneVot.Pentru);
        var impotriva = punct.Voturi.Count(v => v.Optiune == OptiuneVot.Impotriva);
        var abtinere = punct.Voturi.Count(v => v.Optiune == OptiuneVot.Abtinere);

        var hcl = new Hcl
        {
            TipHcl = dto.TipHcl,
            Titlu = punct.Titlu,
            DataAdoptare = sedinta.DataOra,
            Status = StatusHclRedactional.Draft,
            EstePublicat = false,
            PunctOrdineZiId = punct.Id,
            VotPentru = pentru,
            VotImpotriva = impotriva,
            VotAbtinere = abtinere,
            TipMajoritate = punct.TipMajoritate.Value
        };

        hcl.Semnatari.Add(new SemnatarHcl
        {
            RolSemnatar = RolSemnatar.PresedinteSedinta,
            ConsilierId = sedinta.PresedinteSedintaConsilierId.Value,
            DataSemnare = dataAdoptareLocala,
            OrdineAfisare = 1
        });
        hcl.Semnatari.Add(new SemnatarHcl
        {
            RolSemnatar = RolSemnatar.SecretarUat,
            PersoanaId = secretarUat.Id,
            DataSemnare = dataAdoptareLocala,
            OrdineAfisare = 2
        });

        _context.Hcluri.Add(hcl);
        await _context.SaveChangesAsync();

        // Reload cu navigările pentru generator (aceeași instanță tracked, navigări populate)
        hcl = await _context.Hcluri
            .Include(h => h.PunctOrdineZi).ThenInclude(p => p.Sedinta).ThenInclude(s => s.Institutie)
            .Include(h => h.Semnatari).ThenInclude(s => s.Persoana)
            .Include(h => h.Semnatari).ThenInclude(s => s.Consilier)
            .Include(h => h.Documente)
            .Include(h => h.RelatiiSursa).ThenInclude(r => r.HclTinta)
            .FirstAsync(h => h.Id == hcl.Id);

        hcl.Continut = _generator.GenereazaContinut(hcl);
        await _context.SaveChangesAsync();

        return Ok(MapeazaSpreDto(hcl));
    }

    [HttpPut("{id}/Continut")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> EditeazaContinut(int id, EditareContinutHclDto dto)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == id);
        if (hcl is null) return NotFound();
        if (hcl.Status == StatusHclRedactional.Semnat)
            return Conflict("HCL semnat — conținutul nu mai poate fi editat.");

        hcl.Continut = dto.Continut;
        await _context.SaveChangesAsync();
        return Ok(MapareHcl.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    [HttpPost("{id}/RegenereazaContinut")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> RegenereazaContinut(int id)
    {
        var hcl = await _context.Hcluri
            .Include(h => h.PunctOrdineZi).ThenInclude(p => p.Sedinta).ThenInclude(s => s.Institutie)
            .Include(h => h.Semnatari).ThenInclude(s => s.Persoana)
            .Include(h => h.Semnatari).ThenInclude(s => s.Consilier)
            .Include(h => h.Documente)
            .Include(h => h.RelatiiSursa).ThenInclude(r => r.HclTinta)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hcl is null) return NotFound();
        if (hcl.Status == StatusHclRedactional.Semnat)
            return Conflict("HCL semnat — conținutul nu mai poate fi regenerat.");

        hcl.Continut = _generator.GenereazaContinut(hcl);
        await _context.SaveChangesAsync();
        return Ok(MapareHcl.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    [HttpPost("{id}/AtribuieNumar")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> AtribuieNumar(int id, AtribuireNumarHclDto dto)
    {
        var rezultat = await _numerotare.AtribuieAsync(id, dto.Numar, dto.ConfirmaCuLacune);

        switch (rezultat.Tip)
        {
            case TipRezultatAtribuire.Succes:
                return Ok(MapareHcl.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
            case TipRezultatAtribuire.HclInexistent:
                return NotFound(rezultat.MesajEroare);
            case TipRezultatAtribuire.NumarInvalid:
                return BadRequest(rezultat.MesajEroare);
            case TipRezultatAtribuire.StareInvalidaHcl:
                return Conflict(rezultat.MesajEroare);
            case TipRezultatAtribuire.LacuneNeconfirmate:
                return Conflict(new { mesaj = rezultat.MesajEroare, lacune = rezultat.Lacune });
            case TipRezultatAtribuire.NumarLuat:
                return Conflict(new { mesaj = rezultat.MesajEroare, sugestieAlternativa = rezultat.SugestieAlternativa });
            default:
                return Conflict(rezultat.MesajEroare);
        }
    }

    // Pre-completează dialogul de numerotare cu următorul număr liber (sare peste numerele arse).
    // An = anul juridic LOCAL al adoptării, identic cu ServiciuNumerotareHcl.AtribuieAsync.
    [HttpGet("{id}/SugestieNumar")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> SugestieNumar(int id, CancellationToken ct)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (hcl is null) return NotFound();

        var fusOrar = await _context.Institutii
            .Where(i => i.Id == hcl.InstitutieId)
            .Select(i => i.FusOrar)
            .FirstAsync(ct);
        var an = hcl.DataAdoptare.LaFusOrar(fusOrar).Year;
        var numar = await _numerotare.SugereazaNumarAsync(hcl.InstitutieId, an, ct);

        return Ok(new SugestieNumarDto(numar, an));
    }

    [HttpPost("{id}/Semneaza")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Semneaza(int id)
    {
        var hcl = await _context.Hcluri
            .Include(h => h.Semnatari)
            .FirstOrDefaultAsync(h => h.Id == id);
        if (hcl is null) return NotFound();

        if (hcl.Status != StatusHclRedactional.Numerotat)
            return Conflict($"Semnarea e posibilă doar din starea Numerotat (curent: {hcl.Status}).");

        var secretari = hcl.Semnatari.Count(s => s.RolSemnatar == RolSemnatar.SecretarUat);
        var presedinti = hcl.Semnatari.Count(s => s.RolSemnatar == RolSemnatar.PresedinteSedinta);
        var alternativi = hcl.Semnatari.Count(s => s.RolSemnatar == RolSemnatar.SemnatarAlternativArt140);

        if (secretari != 1)
            return BadRequest("Trebuie exact un semnatar Secretar UAT.");

        var arePresedinte = presedinti == 1;
        var areAlternativi = alternativi >= 2
            && !string.IsNullOrWhiteSpace(hcl.MotivLipsaSemnaturaPresedinte);

        if (!arePresedinte && !areAlternativi)
            return BadRequest("Trebuie fie un președinte de ședință, fie minim 2 semnatari alternativi (art. 140 alin. 2) cu motivul lipsei semnăturii completat.");

        hcl.Status = StatusHclRedactional.Semnat;
        await _context.SaveChangesAsync();
        return Ok(MapareHcl.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    [HttpPost("{id}/Invalidare")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Invalidare(int id, InvalidareHclDto dto)
    {
        var hcl = await _context.Hcluri
            .Include(h => h.RelatiiSursa).ThenInclude(r => r.HclTinta)
            .Include(h => h.RelatiiTinta).ThenInclude(r => r.HclSursa)
            .FirstOrDefaultAsync(h => h.Id == id);
        if (hcl is null) return NotFound();

        if (hcl.DataInvalidare != null)
            return Conflict("HCL-ul este deja invalidat.");

        var relatiiSursa = hcl.RelatiiSursa.ToList();
        var relatiiTinta = hcl.RelatiiTinta.ToList();

        if ((relatiiSursa.Any() || relatiiTinta.Any()) && !dto.ConfirmaCuRelatiiActive)
        {
            return Conflict(new
            {
                mesaj = "HCL-ul are relații active cu alte hotărâri. Confirmă cu ConfirmaCuRelatiiActive=true pentru a continua.",
                relatiiSursaActive = relatiiSursa.Select(MapareHcl.SpreRelatieDto).ToList(),
                relatiiTintaActive = relatiiTinta.Select(MapareHcl.SpreRelatieDto).ToList()
            });
        }

        hcl.DataInvalidare = DateTime.UtcNow;
        hcl.InvalidatDe = _context.UserIdCurent;
        hcl.MotivInvalidare = dto.Motiv;
        hcl.RefInvalidare = string.IsNullOrWhiteSpace(dto.RefInvalidare) ? null : dto.RefInvalidare.Trim();

        await _context.SaveChangesAsync();
        return Ok(MapareHcl.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    [HttpDelete("{id}/Invalidare")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AnuleazaInvalidare(int id)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == id);
        if (hcl is null) return NotFound();
        if (hcl.DataInvalidare == null)
            return Conflict("HCL-ul nu este invalidat.");

        hcl.DataInvalidare = null;
        hcl.MotivInvalidare = null;
        hcl.RefInvalidare = null;
        hcl.InvalidatDe = null;

        await _context.SaveChangesAsync();
        return Ok(MapareHcl.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    [HttpPut("{id}/Publicare")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Publicare(int id, PublicareHclDto dto)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == id);
        if (hcl is null) return NotFound();

        if (dto.EstePublicat && hcl.Status < StatusHclRedactional.Numerotat)
            return Conflict("HCL-ul poate fi publicat doar după ce a primit număr (Status >= Numerotat).");

        hcl.EstePublicat = dto.EstePublicat;
        await _context.SaveChangesAsync();
        return Ok(MapareHcl.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    [HttpPut("{id}/PublicareMol")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> PublicareMol(int id, PublicareMolDto dto)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == id);
        if (hcl is null) return NotFound();
        if (hcl.Status != StatusHclRedactional.Semnat)
            return Conflict("Publicarea în MOL e posibilă doar pentru HCL semnat.");

        hcl.DataPublicareMol = dto.DataPublicareMol;
        hcl.PublicataDe = _context.UserIdCurent;
        await _context.SaveChangesAsync();
        return Ok(MapareHcl.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    [HttpDelete("{id}/PublicareMol")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AnuleazaPublicareMol(int id)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == id);
        if (hcl is null) return NotFound();
        if (hcl.DataPublicareMol == null)
            return Conflict("HCL-ul nu are dată de publicare în MOL.");

        hcl.DataPublicareMol = null;
        hcl.PublicataDe = null;
        await _context.SaveChangesAsync();
        return Ok(MapareHcl.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    // Matricea DELETE = 4 gărzi ordonate cu early-return (NU switch pe 6 stări;
    // Invalidat/Publicat/Comunicari sunt ortogonale față de Status).
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Sterge(int id)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == id);
        if (hcl is null) return NotFound();

        // 1. Comunicări active → 409 (registrul prefectului inviolabil; ar cascada la soft-delete)
        if (await _context.ComunicariHclPrefect.AnyAsync(c => c.HclId == id))
            return Conflict("HCL-ul nu poate fi șters: există comunicări către prefect în registru (audit inviolabil).");

        // 2. Invalidat → OK (override: act mort legal, eliminare la cererea instanței)
        if (hcl.DataInvalidare != null)
        {
            _context.Hcluri.Remove(hcl);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // 3. Semnat → 409 (act juridic finalizat — corecții doar prin Erată, Faza 7)
        if (hcl.Status == StatusHclRedactional.Semnat)
            return Conflict("HCL-ul nu poate fi șters: este semnat (act juridic finalizat).");

        // 4. Publicat → 409 (depublică întâi)
        if (hcl.EstePublicat)
            return Conflict("HCL-ul nu poate fi șters cât e publicat. Depublică întâi prin PUT /Publicare cu EstePublicat=false.");

        // Draft / Numerotat fără comunicări → OK (numărul rămâne ars în registru)
        _context.Hcluri.Remove(hcl);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}/MotivLipsaPresedinte")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> SeteazaMotivLipsaPresedinte(int id, MotivLipsaPresedinteDto dto)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == id);
        if (hcl is null) return NotFound();
        if (hcl.Status == StatusHclRedactional.Semnat)
            return Conflict("HCL semnat — motivul lipsei semnăturii nu mai poate fi modificat.");
        if (string.IsNullOrWhiteSpace(dto.Motiv))
            return BadRequest("Motivul este obligatoriu.");
        if (dto.Motiv.Trim().Length > 500)
            return BadRequest("Motivul nu poate depăși 500 de caractere.");

        hcl.MotivLipsaSemnaturaPresedinte = dto.Motiv.Trim();
        await _context.SaveChangesAsync();
        return Ok(MapareHcl.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    // ============ Reload cu include ============

    // Reload cu include-urile complete (sursa = MapareHcl.CuIncludeComplet) pentru toate
    // acțiunile care întorc HclDetaliiDto: editare/regenerare conținut, atribuire număr,
    // semnare, încărcare semnat + mutațiile de stări legale (publicare/MOL/invalidare/motiv).
    private Task<Hcl> ReincarcaCuIncludeAsync(int id, CancellationToken ct = default) =>
        _context.Hcluri.CuIncludeComplet().FirstAsync(h => h.Id == id, ct);

    // ============ Mappers ============

    // HclDto (slim) — folosit la Lista și Genereaza (care navighează la /hcl/:id, ce reîncarcă
    // Detalii). Maparea spre HclDetaliiDto stă în MapareHcl (partajată cu SemnatariHclController).
    private static HclDto MapeazaSpreDto(Hcl h) => new(
        h.Id, h.Numar, h.AnNumerotare, h.TipHcl, h.Titlu,
        h.DataAdoptare, h.DataIntrareInVigoare, h.Status,
        h.EstePublicat, h.DataPublicareMol,
        h.DataInvalidare, h.MotivInvalidare, h.InstitutieId, h.CreatLa);
}