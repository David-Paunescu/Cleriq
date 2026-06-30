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
public class DispozitiiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IServiciuFunctiiIstorice _functiiIstorice;
    private readonly IServiciuNumerotareActe _numerotare;
    private readonly IGeneratorDispozitie _generator;
    private readonly IStocareDocumente _stocare;

    public DispozitiiController(
        AppDbContext context,
        IServiciuFunctiiIstorice functiiIstorice,
        IServiciuNumerotareActe numerotare,
        IGeneratorDispozitie generator,
        IStocareDocumente stocare)
    {
        _context = context;
        _functiiIstorice = functiiIstorice;
        _numerotare = numerotare;
        _generator = generator;
        _stocare = stocare;
    }

    [HttpGet]
    public async Task<IActionResult> Lista(
        [FromQuery] int? an,
        [FromQuery] StatusActRedactional? status,
        [FromQuery] TipDispozitie? tip,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        if (take <= 0 || take > 200) take = 50;
        if (skip < 0) skip = 0;

        var query = _context.Dispozitii.AsQueryable();
        if (an.HasValue) query = query.Where(d => d.AnNumerotare == an.Value);
        if (status.HasValue) query = query.Where(d => d.Status == status.Value);
        if (tip.HasValue) query = query.Where(d => d.TipDispozitie == tip.Value);

        var dispozitii = await query
            .OrderByDescending(d => d.DataEmitere)
            .ThenByDescending(d => d.Id)
            .Skip(skip).Take(take)
            .ToListAsync();

        return Ok(dispozitii.Select(MapeazaSpreDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalii(int id)
    {
        var dispozitie = await _context.Dispozitii.CuIncludeComplet().FirstOrDefaultAsync(d => d.Id == id);
        if (dispozitie is null) return NotFound();
        return Ok(MapareDispozitie.SpreDetaliiDto(dispozitie));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Creeaza(CreareDispozitieDto dto)
    {
        var titlu = dto.Titlu?.Trim();
        if (string.IsNullOrWhiteSpace(titlu))
            return BadRequest("Titlul dispoziției este obligatoriu.");

        // DataEmitere e DateTime pe entitate (paritate fus-orar la numerotare). Input-ul e o dată
        // calendaristică → o fixăm la prânz UTC: anul de registru (.LaFusOrar(fus).Year) rămâne
        // corect pentru fusul RO (UTC+2/+3), cu tampon de 12h față de miezul nopții.
        var dataEmitereUtc = dto.DataEmitere.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);

        // Emitent — derivat din primarul de la data emiterii SAU override manual (viceprimar înlocuitor)
        int? emitentPersoanaId = null;
        int? emitentConsilierId = null;
        if (dto.EmitentConsilierId.HasValue)
        {
            var consilier = await _context.Consilieri
                .FirstOrDefaultAsync(c => c.Id == dto.EmitentConsilierId.Value);
            if (consilier is null)
                return BadRequest("Consilierul indicat ca emitent (înlocuitor de drept) nu există.");
            emitentConsilierId = consilier.Id;
        }
        else
        {
            var primar = await _functiiIstorice.CinEPrimarulLa(dto.DataEmitere);
            if (primar is null)
                return BadRequest("Nu există primar valid la data emiterii. Trimite emitentConsilierId "
                    + "(viceprimar înlocuitor de drept) sau adaugă mandatul de primar în Funcții oficiale.");
            emitentPersoanaId = primar.Id;
        }

        // Secretar contrasemnatar — derivat din CinESecretarulUatLa; 400 conștient dacă null
        // (substitutul consilier-juridic e amânat — vezi plan, Decizii de domeniu #2).
        var secretar = await _functiiIstorice.CinESecretarulUatLa(dto.DataEmitere);
        if (secretar is null)
            return BadRequest("Nu există Secretar UAT valid la data emiterii. Verifică Funcții oficiale "
                + "și adaugă mandatul corespunzător.");

        var dispozitie = new Dispozitie
        {
            TipDispozitie = dto.TipDispozitie,
            Titlu = titlu,
            DataEmitere = dataEmitereUtc,
            Status = StatusActRedactional.Draft,
            EstePublicat = false
        };

        dispozitie.Semnatari.Add(new SemnatarDispozitie
        {
            RolSemnatar = RolSemnatarDispozitie.Emitent,
            PersoanaId = emitentPersoanaId,
            ConsilierId = emitentConsilierId,
            DataSemnare = dto.DataEmitere,
            OrdineAfisare = 1
        });
        dispozitie.Semnatari.Add(new SemnatarDispozitie
        {
            RolSemnatar = RolSemnatarDispozitie.SecretarContrasemnatura,
            PersoanaId = secretar.Id,
            DataSemnare = dto.DataEmitere,
            OrdineAfisare = 2
        });

        _context.Dispozitii.Add(dispozitie);
        await _context.SaveChangesAsync();

        // Reload cu navigările pentru generator (aceeași instanță tracked, navigări populate)
        dispozitie = await _context.Dispozitii
            .Include(d => d.Institutie)
            .Include(d => d.Semnatari).ThenInclude(s => s.Persoana)
            .Include(d => d.Semnatari).ThenInclude(s => s.Consilier)
            .FirstAsync(d => d.Id == dispozitie.Id);

        dispozitie.Continut = _generator.GenereazaContinut(dispozitie);
        await _context.SaveChangesAsync();

        return Ok(MapareDispozitie.SpreDetaliiDto(await ReincarcaCuIncludeAsync(dispozitie.Id)));
    }

    [HttpPut("{id}/Continut")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> EditeazaContinut(int id, EditareContinutDispozitieDto dto)
    {
        var dispozitie = await _context.Dispozitii.FirstOrDefaultAsync(d => d.Id == id);
        if (dispozitie is null) return NotFound();
        if (dispozitie.Status == StatusActRedactional.Semnat)
            return Conflict("Dispoziție semnată — conținutul nu mai poate fi editat.");

        dispozitie.Continut = dto.Continut;
        await _context.SaveChangesAsync();
        return Ok(MapareDispozitie.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    [HttpPost("{id}/RegenereazaContinut")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> RegenereazaContinut(int id)
    {
        var dispozitie = await _context.Dispozitii
            .Include(d => d.Institutie)
            .Include(d => d.Semnatari).ThenInclude(s => s.Persoana)
            .Include(d => d.Semnatari).ThenInclude(s => s.Consilier)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dispozitie is null) return NotFound();
        if (dispozitie.Status == StatusActRedactional.Semnat)
            return Conflict("Dispoziție semnată — conținutul nu mai poate fi regenerat.");

        dispozitie.Continut = _generator.GenereazaContinut(dispozitie);
        await _context.SaveChangesAsync();
        return Ok(MapareDispozitie.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    [HttpPost("{id}/AtribuieNumar")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> AtribuieNumar(int id, AtribuireNumarDispozitieDto dto)
    {
        var rezultat = await _numerotare.AtribuieAsync<Dispozitie>(id, dto.Numar, dto.ConfirmaCuLacune);

        // TipRezultatAtribuire.HclInexistent / StareInvalidaHcl sunt coduri interne (nume HCL,
        // partajate cu serviciul generic) — le mapăm aici la status + mesaje proprii dispoziției.
        switch (rezultat.Tip)
        {
            case TipRezultatAtribuire.Succes:
                return Ok(MapareDispozitie.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
            case TipRezultatAtribuire.HclInexistent:
                return NotFound("Dispoziția nu există.");
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
    // An = anul juridic LOCAL al emiterii, identic cu ServiciuNumerotareActe.AtribuieAsync.
    [HttpGet("{id}/SugestieNumar")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> SugestieNumar(int id, CancellationToken ct)
    {
        var dispozitie = await _context.Dispozitii.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (dispozitie is null) return NotFound();

        var fusOrar = await _context.Institutii
            .Where(i => i.Id == dispozitie.InstitutieId)
            .Select(i => i.FusOrar)
            .FirstAsync(ct);
        var an = dispozitie.DataEmitere.LaFusOrar(fusOrar).Year;
        var numar = await _numerotare.SugereazaNumarAsync<Dispozitie>(dispozitie.InstitutieId, an, ct);

        return Ok(new SugestieNumarDto(numar, an));
    }

    [HttpPost("{id}/Semneaza")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Semneaza(int id)
    {
        var dispozitie = await _context.Dispozitii
            .Include(d => d.Semnatari)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (dispozitie is null) return NotFound();

        if (dispozitie.Status != StatusActRedactional.Numerotat)
            return Conflict($"Semnarea e posibilă doar din starea Numerotat (curent: {dispozitie.Status}).");

        // Gardă de completitudine: exact 1 Emitent + (contrasemnătura secretarului SAU refuz motivat).
        // Semnatarii soft-șterși (rândul de secretar la refuz) nu se numără — filtrul global îi exclude.
        var emitenti = dispozitie.Semnatari.Count(s => s.RolSemnatar == RolSemnatarDispozitie.Emitent);
        var secretari = dispozitie.Semnatari.Count(s => s.RolSemnatar == RolSemnatarDispozitie.SecretarContrasemnatura);

        if (emitenti != 1)
            return BadRequest("Trebuie exact un semnatar Emitent (primar sau înlocuitor de drept).");

        var areContrasemnatura = secretari == 1;
        var areRefuzMotivat = dispozitie.ContrasemnaturaRefuzata
            && !string.IsNullOrWhiteSpace(dispozitie.ObiectieLegalitateSecretar);

        if (!areContrasemnatura && !areRefuzMotivat)
            return BadRequest("Trebuie fie contrasemnătura secretarului general, fie un refuz de "
                + "contrasemnare cu obiecție de legalitate motivată (art. 197 alin. (3) Cod adm.).");

        dispozitie.Status = StatusActRedactional.Semnat;
        await _context.SaveChangesAsync();
        return Ok(MapareDispozitie.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    // Refuzul secretarului de a contrasemna pentru nelegalitate (art. 197 alin. (3)): consemnează
    // obiecția motivată (autor + dată) ȘI soft-șterge rândul de secretar contrasemnatar. Primarul
    // poate apoi emite pe răspundere proprie peste refuz (ramura „SAU refuz" din Semneaza).
    [HttpPost("{id}/RefuzContrasemnare")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> RefuzContrasemnare(int id, RefuzContrasemnareDto dto)
    {
        var dispozitie = await _context.Dispozitii
            .Include(d => d.Semnatari)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (dispozitie is null) return NotFound();

        if (dispozitie.Status == StatusActRedactional.Semnat)
            return Conflict("Dispoziție semnată — refuzul de contrasemnare nu se mai poate înregistra.");

        var obiectie = dto.ObiectieLegalitate?.Trim();
        if (string.IsNullOrWhiteSpace(obiectie))
            return BadRequest("Obiecția de legalitate motivată este obligatorie la refuzul contrasemnării.");
        if (obiectie.Length > 2000)
            return BadRequest("Obiecția de legalitate nu poate depăși 2000 de caractere.");

        if (dispozitie.ContrasemnaturaRefuzata)
            return Conflict("Contrasemnarea a fost deja refuzată pentru această dispoziție.");

        dispozitie.ContrasemnaturaRefuzata = true;
        dispozitie.ObiectieLegalitateSecretar = obiectie;
        dispozitie.RefuzContrasemnareDe = _context.UserIdCurent;
        dispozitie.DataRefuzContrasemnare = DateTime.UtcNow;

        // Actul nu mai poartă contrasemnătura → eliberăm și rândul de secretar (soft-delete), altfel
        // garda de la Semneaza l-ar număra drept contrasemnat în loc să cadă pe ramura de refuz.
        var secretar = dispozitie.Semnatari
            .FirstOrDefault(s => s.RolSemnatar == RolSemnatarDispozitie.SecretarContrasemnatura);
        if (secretar != null)
            _context.SemnatariDispozitie.Remove(secretar);

        await _context.SaveChangesAsync();
        return Ok(MapareDispozitie.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    // ============ Variantă semnată (Nivel 1: scan PDF) ============

    [HttpPost("{id}/Semnat")]
    [Authorize(Roles = "Admin,Secretar")]
    [RequestSizeLimit(ValidareDocument.MarimeMaxima)]
    public async Task<IActionResult> IncarcaSemnat(int id, [FromForm] IFormFile fisier, CancellationToken ct)
    {
        var dispozitie = await _context.Dispozitii.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (dispozitie is null) return NotFound();

        // Gărzi de freeze inline (paritar HCL): doar pe Semnat + îngheț după intrarea în circuit.
        if (dispozitie.Status != StatusActRedactional.Semnat)
            return Conflict("Doar o dispoziție semnată poate primi varianta PDF semnată.");
        if (dispozitie.CaleStocareSemnat != null && dispozitie.AIntratInCircuit)
            return Conflict("Dispoziție intrată în circuit (publicată în MOL / comunicată) — "
                + "varianta semnată nu mai poate fi înlocuită.");

        var eroare = VariantaSemnata.ValideazaPdf(fisier, "dispoziția semnată");
        if (eroare != null) return BadRequest(eroare);

        var caleVeche = await VariantaSemnata.StocheazaAsync(
            dispozitie, fisier, _stocare, _context.InstitutieIdCurenta, ct);
        await _context.SaveChangesAsync(ct);
        await VariantaSemnata.StergeVecheAsync(_stocare, caleVeche, dispozitie.CaleStocareSemnat!, ct);

        return Ok(MapareDispozitie.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id, ct)));
    }

    [HttpGet("{id}/Semnat")]
    public async Task<IActionResult> DescarcaSemnat(int id, CancellationToken ct)
    {
        var dispozitie = await _context.Dispozitii.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (dispozitie is null || string.IsNullOrEmpty(dispozitie.CaleStocareSemnat))
            return NotFound("Nu există variantă semnată încărcată.");

        Stream stream;
        try
        {
            stream = await _stocare.DeschideAsync(dispozitie.CaleStocareSemnat, ct);
        }
        catch (FileNotFoundException)
        {
            return NotFound("Fișierul fizic lipsește.");
        }

        return File(stream, "application/pdf", dispozitie.NumeFisierSemnat ?? "dispozitie-semnata.pdf");
    }

    [HttpDelete("{id}/Semnat")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> StergeSemnat(int id, CancellationToken ct)
    {
        var dispozitie = await _context.Dispozitii.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (dispozitie is null || string.IsNullOrEmpty(dispozitie.CaleStocareSemnat))
            return NotFound("Nu există variantă semnată încărcată.");

        if (dispozitie.AIntratInCircuit)
            return Conflict("Dispoziție intrată în circuit — varianta semnată nu mai poate fi ștearsă.");

        VariantaSemnata.Curata(dispozitie);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // ============ Invalidare / revocare ============

    [HttpPost("{id}/Invalidare")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Invalidare(int id, InvalidareDispozitieDto dto)
    {
        var dispozitie = await _context.Dispozitii.FirstOrDefaultAsync(d => d.Id == id);
        if (dispozitie is null) return NotFound();

        if (!Enum.IsDefined(dto.Motiv))
            return BadRequest("Motiv de invalidare necunoscut.");

        var motivAltulText = dto.MotivAltulText?.Trim();
        if (dto.Motiv == MotivInvalidare.Altul)
        {
            if (string.IsNullOrWhiteSpace(motivAltulText))
                return BadRequest("Pentru motivul „Altul” este obligatoriu un text explicativ.");
            if (motivAltulText.Length > 300)
                return BadRequest("Textul motivului nu poate depăși 300 de caractere.");
        }

        if (dispozitie.DataInvalidare != null)
            return Conflict("Dispoziția este deja invalidată.");

        // Regula de revocare proprie (Retractat): admisibilă oricând la Normativ, dar DOAR înainte de
        // intrarea în circuit la Individual (după → doar anulare de instanță, art. 1 alin. (6) L554/2004).
        if (dto.Motiv == MotivInvalidare.Retractat
            && dispozitie.TipDispozitie == TipDispozitie.Individual
            && dispozitie.AIntratInCircuit)
        {
            return Conflict("Dispoziția individuală a intrat în circuit și a produs efecte juridice — nu "
                + "mai poate fi revocată de primar. Singura cale este anularea de către instanța de "
                + "contencios administrativ (motiv „Anulat de instanță”).");
        }

        dispozitie.DataInvalidare = DateTime.UtcNow;
        dispozitie.InvalidatDe = _context.UserIdCurent;
        dispozitie.MotivInvalidare = dto.Motiv;
        dispozitie.MotivInvalidareAltulText = dto.Motiv == MotivInvalidare.Altul ? motivAltulText : null;
        dispozitie.RefInvalidare = string.IsNullOrWhiteSpace(dto.RefInvalidare) ? null : dto.RefInvalidare.Trim();

        await _context.SaveChangesAsync();
        return Ok(MapareDispozitie.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    [HttpDelete("{id}/Invalidare")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AnuleazaInvalidare(int id)
    {
        var dispozitie = await _context.Dispozitii.FirstOrDefaultAsync(d => d.Id == id);
        if (dispozitie is null) return NotFound();
        if (dispozitie.DataInvalidare == null)
            return Conflict("Dispoziția nu este invalidată.");

        dispozitie.DataInvalidare = null;
        dispozitie.MotivInvalidare = null;
        dispozitie.MotivInvalidareAltulText = null;
        dispozitie.RefInvalidare = null;
        dispozitie.InvalidatDe = null;

        await _context.SaveChangesAsync();
        return Ok(MapareDispozitie.SpreDetaliiDto(await ReincarcaCuIncludeAsync(id)));
    }

    // Matricea DELETE = gărzi ordonate cu early-return (paritar HCL).
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Sterge(int id)
    {
        var dispozitie = await _context.Dispozitii.FirstOrDefaultAsync(d => d.Id == id);
        if (dispozitie is null) return NotFound();

        // (Pas 10) Comunicări active la prefect → 409: registrul e inviolabil (ar cascada la soft-delete).

        // Invalidat → OK (override: act mort legal, eliminare la cererea instanței, chiar și pe semnat)
        if (dispozitie.DataInvalidare != null)
        {
            _context.Dispozitii.Remove(dispozitie);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Semnat → 409 (act juridic finalizat — corecții doar prin Erată, Faza 7)
        if (dispozitie.Status == StatusActRedactional.Semnat)
            return Conflict("Dispoziția nu poate fi ștearsă: este semnată (act juridic finalizat).");

        // Publicat → 409 (depublică întâi)
        if (dispozitie.EstePublicat)
            return Conflict("Dispoziția nu poate fi ștearsă cât e publicată. Depublică întâi.");

        // Draft / Numerotat fără dependințe → OK (numărul rămâne ars în registru)
        _context.Dispozitii.Remove(dispozitie);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ============ Reload cu include ============

    private Task<Dispozitie> ReincarcaCuIncludeAsync(int id, CancellationToken ct = default) =>
        _context.Dispozitii.CuIncludeComplet().FirstAsync(d => d.Id == id, ct);

    // ============ Mappers ============

    // DispozitieDto (slim) — folosit la Lista. Maparea spre DispozitieDetaliiDto stă în MapareDispozitie.
    private static DispozitieDto MapeazaSpreDto(Dispozitie d) => new(
        d.Id, d.Numar, d.AnNumerotare, d.TipDispozitie, d.Titlu,
        d.DataEmitere, d.DataIntrareInVigoare, d.Status,
        d.EstePublicat, d.DataPublicareMol,
        d.DataInvalidare, d.MotivInvalidare, d.InstitutieId, d.CreatLa);
}
