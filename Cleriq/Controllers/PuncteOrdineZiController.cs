using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/Sedinte/{sedintaId}/Puncte")]
[Authorize]
public class PuncteOrdineZiController : ControllerBase
{
    private readonly AppDbContext _context;

    public PuncteOrdineZiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Lista(int sedintaId)
    {
        var sedintaExista = await _context.Sedinte.AnyAsync(s => s.Id == sedintaId);
        if (!sedintaExista)
            return NotFound("Ședința nu există.");

        var puncte = await _context.PuncteOrdineZi
            .Where(p => p.SedintaId == sedintaId)
            .OrderBy(p => p.Ordine)
            .Select(p => new PunctOrdineZiDto(
                p.Id, p.SedintaId, p.Ordine, p.Titlu, p.Descriere, p.Tip,
                p.NecesitaVot, p.TipMajoritate, p.Rezultat, p.InstitutieId, p.CreatLa))
            .ToListAsync();

        return Ok(puncte);
    }

    [HttpGet("{punctId}")]
    public async Task<IActionResult> Detalii(int sedintaId, int punctId)
    {
        var punct = await _context.PuncteOrdineZi
            .FirstOrDefaultAsync(p => p.Id == punctId && p.SedintaId == sedintaId);
        if (punct is null)
            return NotFound();

        return Ok(MapeazaSpreDto(punct));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Creeaza(int sedintaId, CrearePunctDto dto)
    {
        var sedintaExista = await _context.Sedinte.AnyAsync(s => s.Id == sedintaId);
        if (!sedintaExista)
            return NotFound("Ședința nu există.");

        var eroare = ValideazaConsistenta(dto.NecesitaVot, dto.TipMajoritate);
        if (eroare is not null)
            return BadRequest(eroare);

        var ordineLuata = await _context.PuncteOrdineZi
            .AnyAsync(p => p.SedintaId == sedintaId && p.Ordine == dto.Ordine);
        if (ordineLuata)
            return Conflict($"Există deja un punct cu ordinea {dto.Ordine} în această ședință.");

        var punct = new PunctOrdineZi
        {
            SedintaId = sedintaId,
            Ordine = dto.Ordine,
            Titlu = dto.Titlu,
            Descriere = dto.Descriere,
            Tip = dto.Tip,
            NecesitaVot = dto.NecesitaVot,
            TipMajoritate = dto.NecesitaVot ? dto.TipMajoritate : null
            // Rezultat rămâne null — se setează doar la închiderea votului
        };

        _context.PuncteOrdineZi.Add(punct);
        await _context.SaveChangesAsync();

        return Ok(MapeazaSpreDto(punct));
    }

    [HttpPut("{punctId}")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Actualizeaza(int sedintaId, int punctId, ActualizarePunctDto dto)
    {
        var punct = await _context.PuncteOrdineZi
            .FirstOrDefaultAsync(p => p.Id == punctId && p.SedintaId == sedintaId);
        if (punct is null)
            return NotFound();

        var eroare = ValideazaConsistenta(dto.NecesitaVot, dto.TipMajoritate);
        if (eroare is not null)
            return BadRequest(eroare);

        var ordineLuata = await _context.PuncteOrdineZi
            .AnyAsync(p => p.SedintaId == sedintaId
                        && p.Ordine == dto.Ordine
                        && p.Id != punctId);
        if (ordineLuata)
            return Conflict($"Există deja un punct cu ordinea {dto.Ordine} în această ședință.");

        punct.Ordine = dto.Ordine;
        punct.Titlu = dto.Titlu;
        punct.Descriere = dto.Descriere;
        punct.Tip = dto.Tip;
        punct.NecesitaVot = dto.NecesitaVot;
        punct.TipMajoritate = dto.NecesitaVot ? dto.TipMajoritate : null;
        // Rezultat NU se modifică aici

        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(punct));
    }

    [HttpDelete("{punctId}")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Sterge(int sedintaId, int punctId)
    {
        var punct = await _context.PuncteOrdineZi
            .FirstOrDefaultAsync(p => p.Id == punctId && p.SedintaId == sedintaId);
        if (punct is null)
            return NotFound();

        _context.PuncteOrdineZi.Remove(punct);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static string? ValideazaConsistenta(bool necesitaVot, TipMajoritate? tipMajoritate)
    {
        if (necesitaVot && !tipMajoritate.HasValue)
            return "Dacă punctul necesită vot, TipMajoritate este obligatoriu.";
        return null;
    }

    // new pentru vot
    [HttpPost("{punctId}/Inchide")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> InchideVot(int sedintaId, int punctId)
    {
        var punct = await _context.PuncteOrdineZi
            .FirstOrDefaultAsync(p => p.Id == punctId && p.SedintaId == sedintaId);
        if (punct is null)
            return NotFound("Punctul nu există în această ședință.");

        if (!punct.NecesitaVot)
            return BadRequest("Acest punct nu necesită vot.");

        if (punct.Rezultat.HasValue)
            return Conflict("Votul este deja închis.");

        if (!punct.TipMajoritate.HasValue)
            return BadRequest("Punctul nu are TipMajoritate setat.");

        var pentru = await _context.Voturi
            .CountAsync(v => v.PunctId == punctId && v.Optiune == OptiuneVot.Pentru);
        var impotriva = await _context.Voturi
            .CountAsync(v => v.PunctId == punctId && v.Optiune == OptiuneVot.Impotriva);
        var abtinere = await _context.Voturi
            .CountAsync(v => v.PunctId == punctId && v.Optiune == OptiuneVot.Abtinere);
        var totalExprimate = pentru + impotriva + abtinere;
        var totalInFunctie = await _context.Consilieri.CountAsync(c => c.Activ);

        var (adoptat, prag) = CalculeazaRezultat(
            punct.TipMajoritate.Value, pentru, impotriva, abtinere, totalInFunctie);

        punct.Rezultat = adoptat ? RezultatPunct.Adoptat : RezultatPunct.Respins;
        await _context.SaveChangesAsync();

        return Ok(new RezultatVotDto(
            punct.Id, punct.Rezultat.Value, punct.TipMajoritate,
            totalInFunctie, pentru, impotriva, abtinere, totalExprimate, prag));
    }

    [HttpPost("{punctId}/Amana")]
    [Authorize(Roles = "Admin,Secretar")]
    public Task<IActionResult> Amana(int sedintaId, int punctId)
        => SeteazaRezultatManual(sedintaId, punctId, RezultatPunct.Amanat);

    [HttpPost("{punctId}/Retrage")]
    [Authorize(Roles = "Admin,Secretar")]
    public Task<IActionResult> Retrage(int sedintaId, int punctId)
        => SeteazaRezultatManual(sedintaId, punctId, RezultatPunct.Retras);

    private async Task<IActionResult> SeteazaRezultatManual(
        int sedintaId, int punctId, RezultatPunct rezultat)
    {
        var punct = await _context.PuncteOrdineZi
            .FirstOrDefaultAsync(p => p.Id == punctId && p.SedintaId == sedintaId);
        if (punct is null)
            return NotFound("Punctul nu există în această ședință.");

        if (punct.Rezultat.HasValue)
            return Conflict("Punctul are deja un rezultat setat.");

        punct.Rezultat = rezultat;
        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(punct));
    }

    private static (bool Adoptat, int Prag) CalculeazaRezultat(
        TipMajoritate tip, int pentru, int impotriva, int abtinere, int totalInFunctie)
    {
        return tip switch
        {
            // Simplă: pentru ≥ primul natural > totalExprimate/2
            TipMajoritate.Simpla => CalculeazaSimpla(pentru, impotriva, abtinere),
            // Absolută: pentru ≥ primul natural > totalÎnFuncție/2
            TipMajoritate.Absoluta => CalculeazaAbsoluta(pentru, totalInFunctie),
            // Calificată: pentru ≥ ceil(2 × totalÎnFuncție / 3)
            TipMajoritate.Calificata => CalculeazaCalificata(pentru, totalInFunctie),
            _ => (false, 0)
        };
    }

    private static (bool, int) CalculeazaSimpla(int pentru, int impotriva, int abtinere)
    {
        var totalExprimate = pentru + impotriva + abtinere;
        var prag = (totalExprimate / 2) + 1;
        return (pentru >= prag, prag);
    }

    private static (bool, int) CalculeazaAbsoluta(int pentru, int totalInFunctie)
    {
        var prag = (totalInFunctie / 2) + 1;
        return (pentru >= prag, prag);
    }

    private static (bool, int) CalculeazaCalificata(int pentru, int totalInFunctie)
    {
        // ceil(2n/3) = (2n + 2) / 3 cu integer division
        var prag = (2 * totalInFunctie + 2) / 3;
        return (pentru >= prag, prag);
    }
    //

    private static PunctOrdineZiDto MapeazaSpreDto(PunctOrdineZi p) => new(
        p.Id, p.SedintaId, p.Ordine, p.Titlu, p.Descriere, p.Tip,
        p.NecesitaVot, p.TipMajoritate, p.Rezultat, p.InstitutieId, p.CreatLa);
}