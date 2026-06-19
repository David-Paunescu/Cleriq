using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MandateFunctieController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IServiciuValidareMandate _validare;

    public MandateFunctieController(AppDbContext context, IServiciuValidareMandate validare)
    {
        _context = context;
        _validare = validare;
    }

    [HttpGet]
    public async Task<IActionResult> Lista([FromQuery] TipFunctie? tipFunctie = null)
    {
        var query = _context.MandateFunctie
            .Include(m => m.Persoana)
            .Include(m => m.Consilier)
            .AsQueryable();

        if (tipFunctie.HasValue)
            query = query.Where(m => m.TipFunctie == tipFunctie.Value);

        var mandate = await query
            .OrderByDescending(m => m.DataInceput)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return Ok(mandate.Select(MapeazaSpreDto));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalii(int id)
    {
        var mandat = await _context.MandateFunctie
            .Include(m => m.Persoana)
            .Include(m => m.Consilier)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mandat is null) return NotFound();
        return Ok(MapeazaSpreDto(mandat));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Creeaza(CreareMandatFunctieDto dto)
    {
        var eroareMapare = ValideazaMapare(dto.TipFunctie, dto.PersoanaId, dto.ConsilierId);
        if (eroareMapare is not null) return BadRequest(eroareMapare);

        if (dto.DataSfarsit.HasValue && dto.DataSfarsit.Value < dto.DataInceput)
            return BadRequest("DataSfarsit nu poate fi anterioară DataInceput.");

        if (dto.PersoanaId.HasValue &&
            !await _context.Persoane.AnyAsync(p => p.Id == dto.PersoanaId.Value))
            return NotFound("Persoana nu există.");

        if (dto.ConsilierId.HasValue &&
            !await _context.Consilieri.AnyAsync(c => c.Id == dto.ConsilierId.Value))
            return NotFound("Consilierul nu există.");

        if (dto.TipFunctie == TipFunctie.Viceprimar)
        {
            var pv = await _validare.PoateFiViceprimar(
                dto.ConsilierId!.Value, dto.DataInceput, dto.DataSfarsit, mandatExistentId: null);
            if (!pv.Succes) return BadRequest(pv.MotivEsec);
        }

        var ov = await _validare.VerificaOverlap(
            dto.TipFunctie, dto.DataInceput, dto.DataSfarsit,
            dto.PersoanaId, dto.ConsilierId, mandatExistentId: null);
        if (!ov.Succes) return Conflict(ov.MotivEsec);

        var mandat = new MandatFunctie
        {
            TipFunctie = dto.TipFunctie,
            PersoanaId = dto.PersoanaId,
            ConsilierId = dto.ConsilierId,
            DataInceput = dto.DataInceput,
            DataSfarsit = dto.DataSfarsit,
            NrActNumire = NormalizeazaText(dto.NrActNumire)
        };

        _context.MandateFunctie.Add(mandat);
        await _context.SaveChangesAsync();

        // Re-incarc nav properties pentru DTO (cu nume populat)
        if (mandat.PersoanaId.HasValue)
            await _context.Entry(mandat).Reference(m => m.Persoana).LoadAsync();
        if (mandat.ConsilierId.HasValue)
            await _context.Entry(mandat).Reference(m => m.Consilier).LoadAsync();

        return Ok(MapeazaSpreDto(mandat));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Actualizeaza(int id, ActualizareMandatFunctieDto dto)
    {
        var mandat = await _context.MandateFunctie
            .Include(m => m.Persoana)
            .Include(m => m.Consilier)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mandat is null) return NotFound();

        if (dto.DataSfarsit.HasValue && dto.DataSfarsit.Value < dto.DataInceput)
            return BadRequest("DataSfarsit nu poate fi anterioară DataInceput.");

        // Re-validare cu noua perioadă, exclude propriul mandat din verificare
        if (mandat.TipFunctie == TipFunctie.Viceprimar)
        {
            var pv = await _validare.PoateFiViceprimar(
                mandat.ConsilierId!.Value, dto.DataInceput, dto.DataSfarsit, mandatExistentId: id);
            if (!pv.Succes) return BadRequest(pv.MotivEsec);
        }

        var ov = await _validare.VerificaOverlap(
            mandat.TipFunctie, dto.DataInceput, dto.DataSfarsit,
            mandat.PersoanaId, mandat.ConsilierId, mandatExistentId: id);
        if (!ov.Succes) return Conflict(ov.MotivEsec);

        mandat.DataInceput = dto.DataInceput;
        mandat.DataSfarsit = dto.DataSfarsit;
        mandat.NrActNumire = NormalizeazaText(dto.NrActNumire);

        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(mandat));
    }

    [HttpPost("{id}/Inchide")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Inchide(int id, InchideMandatFunctieDto dto)
    {
        var mandat = await _context.MandateFunctie
            .Include(m => m.Persoana)
            .Include(m => m.Consilier)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mandat is null) return NotFound();

        if (mandat.DataSfarsit.HasValue)
            return Conflict("Mandatul este deja închis. Pentru editare folosește PUT.");

        if (dto.DataSfarsit < mandat.DataInceput)
            return BadRequest("DataSfarsit nu poate fi anterioară DataInceput.");

        // Închidere = restrângerea perioadei, NU poate crea overlap nou.
        // NU re-validăm PoateFiViceprimar — Inchide e instrumentul pentru rezolvarea
        // unui viceprimar fantomă (mandat de consilier expirat fără închidere explicită).
        mandat.DataSfarsit = dto.DataSfarsit;
        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(mandat));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Sterge(int id)
    {
        var mandat = await _context.MandateFunctie.FirstOrDefaultAsync(m => m.Id == id);
        if (mandat is null) return NotFound();

        _context.MandateFunctie.Remove(mandat);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static string? ValideazaMapare(TipFunctie tip, int? persoanaId, int? consilierId)
    {
        return tip switch
        {
            TipFunctie.Primar or TipFunctie.SecretarUat =>
                persoanaId.HasValue && !consilierId.HasValue
                    ? null
                    : "Pentru Primar și Secretar UAT este obligatorie PersoanaId, iar ConsilierId trebuie să fie null.",
            TipFunctie.Viceprimar =>
                consilierId.HasValue && !persoanaId.HasValue
                    ? null
                    : "Pentru Viceprimar este obligatoriu ConsilierId, iar PersoanaId trebuie să fie null.",
            _ => "TipFunctie invalid."
        };
    }

    private static string? NormalizeazaText(string? input)
        => string.IsNullOrWhiteSpace(input) ? null : input.Trim();

    private static MandatFunctieDto MapeazaSpreDto(MandatFunctie m) => new(
        m.Id, m.TipFunctie,
        m.PersoanaId, m.Persoana?.NumeComplet,
        m.ConsilierId, m.Consilier?.NumeComplet,
        m.DataInceput, m.DataSfarsit, m.NrActNumire,
        m.InstitutieId, m.CreatLa);
}