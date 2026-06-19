using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Helpers;
using Cleriq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PersoaneController : ControllerBase
{
    private readonly AppDbContext _context;

    public PersoaneController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Lista()
    {
        // Outer query cu filtru global activ — fără IgnoreQueryFilters care ar "scurge".
        var persoane = await _context.Persoane
            .OrderBy(p => p.NumeComplet)
            .ToListAsync();

        // Subquery SEPARAT pentru AreMandate — IgnoreQueryFilters izolat aici.
        var idsCuMandate = (await _context.MandateFunctie
            .IgnoreQueryFilters()
            .Where(mf => mf.PersoanaId != null
                      && mf.InstitutieId == _context.InstitutieIdCurenta)
            .Select(mf => mf.PersoanaId!.Value)
            .Distinct()
            .ToListAsync()).ToHashSet();

        return Ok(persoane.Select(p => MapeazaSpreDto(p, idsCuMandate.Contains(p.Id))));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalii(int id)
    {
        var persoana = await _context.Persoane.FirstOrDefaultAsync(p => p.Id == id);
        if (persoana is null) return NotFound();

        var areMandate = await AreMandateAsync(id);
        return Ok(MapeazaSpreDto(persoana, areMandate));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Creeaza(CrearePersoanaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NumeComplet))
            return BadRequest("Numele complet este obligatoriu.");

        var (telefonNormalizat, eroareTelefon) = NormalizeazaTelefon(dto.Telefon);
        if (eroareTelefon is not null)
            return BadRequest(eroareTelefon);

        var persoana = new Persoana
        {
            NumeComplet = dto.NumeComplet.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            Telefon = telefonNormalizat
        };

        _context.Persoane.Add(persoana);
        await _context.SaveChangesAsync();

        return Ok(MapeazaSpreDto(persoana, areMandate: false));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Actualizeaza(int id, ActualizarePersoanaDto dto)
    {
        var persoana = await _context.Persoane.FirstOrDefaultAsync(p => p.Id == id);
        if (persoana is null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.NumeComplet))
            return BadRequest("Numele complet este obligatoriu.");

        var (telefonNormalizat, eroareTelefon) = NormalizeazaTelefon(dto.Telefon);
        if (eroareTelefon is not null)
            return BadRequest(eroareTelefon);

        persoana.NumeComplet = dto.NumeComplet.Trim();
        persoana.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        persoana.Telefon = telefonNormalizat;

        await _context.SaveChangesAsync();

        var areMandate = await AreMandateAsync(id);
        return Ok(MapeazaSpreDto(persoana, areMandate));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Sterge(int id)
    {
        var persoana = await _context.Persoane.FirstOrDefaultAsync(p => p.Id == id);
        if (persoana is null) return NotFound();

        // Gardă: orice mandat (activ SAU soft-deleted) blochează ștergerea — audit istoric.
        // IgnoreQueryFilters scoate filtrul EsteSters; tenant re-impus manual.
        if (await AreMandateAsync(id))
            return Conflict("Persoana nu poate fi ștearsă: are mandate de funcție oficiale în istoric.");

        _context.Persoane.Remove(persoana);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private Task<bool> AreMandateAsync(int persoanaId)
        => _context.MandateFunctie
            .IgnoreQueryFilters()
            .AnyAsync(mf => mf.PersoanaId == persoanaId
                         && mf.InstitutieId == _context.InstitutieIdCurenta);

    private static (string? Normalizat, string? Eroare) NormalizeazaTelefon(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (null, null);

        var normalizat = Telefon.Normalizeaza(input);
        if (normalizat is null)
            return (null, "Format telefon invalid. Folosește format național (0720123456) sau internațional (+40720123456, 0040720123456).");

        return (normalizat, null);
    }

    private static PersoanaDto MapeazaSpreDto(Persoana p, bool areMandate) =>
        new(p.Id, p.NumeComplet, p.Email, p.Telefon, p.InstitutieId, p.CreatLa, areMandate);
}