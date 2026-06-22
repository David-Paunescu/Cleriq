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

    public HclController(
        AppDbContext context,
        IServiciuFunctiiIstorice functiiIstorice,
        IServiciuNumerotareHcl numerotare,
        IGeneratorHcl generator)
    {
        _context = context;
        _functiiIstorice = functiiIstorice;
        _numerotare = numerotare;
        _generator = generator;
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
        var hcl = await _context.Hcluri
            .Include(h => h.Semnatari).ThenInclude(s => s.Persoana)
            .Include(h => h.Semnatari).ThenInclude(s => s.Consilier)
            .Include(h => h.Documente)
            .Include(h => h.RelatiiSursa).ThenInclude(r => r.HclTinta)
            .Include(h => h.RelatiiTinta).ThenInclude(r => r.HclSursa)
            .Include(h => h.Comunicari)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hcl is null) return NotFound();
        return Ok(MapeazaSpreDetaliiDto(hcl));
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
        return Ok(MapeazaSpreDto(hcl));
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
        return Ok(MapeazaSpreDto(hcl));
    }

    [HttpPost("{id}/AtribuieNumar")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> AtribuieNumar(int id, AtribuireNumarHclDto dto)
    {
        var rezultat = await _numerotare.AtribuieAsync(id, dto.Numar, dto.ConfirmaCuLacune);

        switch (rezultat.Tip)
        {
            case TipRezultatAtribuire.Succes:
                var hcl = await _context.Hcluri.FirstAsync(h => h.Id == id);
                return Ok(MapeazaSpreDto(hcl));
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
        return Ok(MapeazaSpreDto(hcl));
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
                relatiiSursaActive = relatiiSursa.Select(MapeazaRelatie).ToList(),
                relatiiTintaActive = relatiiTinta.Select(MapeazaRelatie).ToList()
            });
        }

        hcl.DataInvalidare = DateTime.UtcNow;
        hcl.InvalidatDe = _context.UserIdCurent;
        hcl.MotivInvalidare = dto.Motiv;
        hcl.RefInvalidare = string.IsNullOrWhiteSpace(dto.RefInvalidare) ? null : dto.RefInvalidare.Trim();

        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(hcl));
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
        return Ok(MapeazaSpreDto(hcl));
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
        return Ok(MapeazaSpreDto(hcl));
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
        return Ok(MapeazaSpreDto(hcl));
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
        return NoContent();
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
        return Ok(MapeazaSpreDto(hcl));
    }

    // ============ Mappers ============

    private static HclDto MapeazaSpreDto(Hcl h) => new(
        h.Id, h.Numar, h.AnNumerotare, h.TipHcl, h.Titlu,
        h.DataAdoptare, h.DataIntrareInVigoare, h.Status,
        h.EstePublicat, h.DataPublicareMol,
        h.DataInvalidare, h.MotivInvalidare, h.InstitutieId, h.CreatLa);

    private static HclDetaliiDto MapeazaSpreDetaliiDto(Hcl h) => new(
        h.Id, h.Numar, h.AnNumerotare, h.TipHcl, h.Titlu, h.Continut,
        h.DataAdoptare, h.DataIntrareInVigoare, h.Status, h.PunctOrdineZiId,
        h.VotPentru, h.VotImpotriva, h.VotAbtinere, h.TipMajoritate,
        h.EstePublicat, h.DataPublicareMol,
        !string.IsNullOrEmpty(h.CaleStocareSemnat), h.NumeFisierSemnat, h.MarimeSemnat, h.DataIncarcareSemnat,
        h.MotivLipsaSemnaturaPresedinte,
        h.DataInvalidare, h.MotivInvalidare, h.RefInvalidare,
        h.InstitutieId, h.CreatLa,
        h.Semnatari.OrderBy(s => s.OrdineAfisare).Select(MapeazaSemnatar).ToList(),
        h.Documente.OrderBy(d => d.Ordine).Select(MapeazaDocument).ToList(),
        h.RelatiiSursa.Select(MapeazaRelatie).ToList(),
        h.RelatiiTinta.Select(MapeazaRelatie).ToList(),
        h.Comunicari.OrderByDescending(c => c.NumarOrdineInRegistru).Select(MapeazaComunicare).ToList());

    private static SemnatarHclDto MapeazaSemnatar(SemnatarHcl s) => new(
        s.Id, s.RolSemnatar, s.PersoanaId, s.ConsilierId,
        s.Persoana?.NumeComplet ?? s.Consilier?.NumeComplet ?? "—",
        s.DataSemnare, s.OrdineAfisare);

    private static DocumentHclDto MapeazaDocument(Document d) => new(
        d.Id, d.Denumire, d.Descriere, d.TipDocumentHcl, d.NumarOrdinAnexa,
        d.NumeFisierOriginal, d.Marime, d.Ordine);

    private static RelatieHclDto MapeazaRelatie(RelatieHcl r) => new(
        r.Id, r.TipRelatie,
        r.HclSursaId, FormateazaNumar(r.HclSursa), r.HclSursa?.Titlu ?? "—",
        r.HclTintaId, FormateazaNumar(r.HclTinta), r.HclTinta?.Titlu,
        r.ReferintaActExternText);

    private static ComunicareHclPrefectDto MapeazaComunicare(ComunicareHclPrefect c) => new(
        c.Id, c.HclId, c.NumarOrdineInRegistru, c.AnRegistru,
        c.DataTrimiteri, c.DataInregistrareInRegistru, c.CanalTransmitere,
        c.NrInregistrarePrefect, c.DataConfirmarePrefect, c.ObiectiiMotivate,
        c.RaspunsPrefect, c.DataRaspunsPrefect, c.ObservatiiInterne, c.CreatLa);

    private static string? FormateazaNumar(Hcl? h)
        => h?.Numar != null && h.AnNumerotare != null ? $"{h.Numar}/{h.AnNumerotare}" : null;
}