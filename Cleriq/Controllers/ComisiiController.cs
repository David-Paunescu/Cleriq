using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComisiiController : ControllerBase
{
    private readonly AppDbContext _context;

    public ComisiiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Lista()
    {
        var comisii = await _context.Comisii.ToListAsync();
        return Ok(comisii.Select(c => new ComisieDto(
            c.Id, c.Denumire, c.Descriere, c.InstitutieId, c.CreatLa,
            new List<MembruComisieDto>())));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalii(int id, [FromQuery] bool includeIstoric = false)
    {
        var comisie = await _context.Comisii.FirstOrDefaultAsync(c => c.Id == id);
        if (comisie is null) return NotFound();

        var query = _context.ComisieMembri
            .Include(m => m.Consilier)
            .Where(m => m.ComisieId == id);

        if (!includeIstoric)
            query = query.Where(m => m.DataSfarsit == null);

        var membri = await query
            .Where(m => m.Consilier != null)
            .OrderBy(m => m.DataInceput)
            .ThenBy(m => m.Id)
            .Select(m => new MembruComisieDto(
                m.ConsilierId,
                m.Consilier!.NumeComplet,
                m.Rol,
                m.DataInceput,
                m.DataSfarsit,
                m.DataInceputEstimata))
            .ToListAsync();

        return Ok(new ComisieDto(
            comisie.Id, comisie.Denumire, comisie.Descriere,
            comisie.InstitutieId, comisie.CreatLa, membri));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Creeaza(CreareComisieDto dto)
    {
        var comisie = new Comisie
        {
            Denumire = dto.Denumire,
            Descriere = dto.Descriere
        };

        _context.Comisii.Add(comisie);
        await _context.SaveChangesAsync();

        return Ok(new ComisieDto(
            comisie.Id, comisie.Denumire, comisie.Descriere,
            comisie.InstitutieId, comisie.CreatLa,
            new List<MembruComisieDto>()));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Actualizeaza(int id, ActualizareComisieDto dto)
    {
        var comisie = await _context.Comisii.FirstOrDefaultAsync(c => c.Id == id);
        if (comisie is null)
            return NotFound();

        comisie.Denumire = dto.Denumire;
        comisie.Descriere = dto.Descriere;
        await _context.SaveChangesAsync();

        return Ok(new ComisieDto(
            comisie.Id, comisie.Denumire, comisie.Descriere,
            comisie.InstitutieId, comisie.CreatLa,
            new List<MembruComisieDto>()));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Sterge(int id)
    {
        var comisie = await _context.Comisii.FirstOrDefaultAsync(c => c.Id == id);
        if (comisie is null)
            return NotFound();

        _context.Comisii.Remove(comisie);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // === Membri ===

    [HttpPost("{id}/Membri")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdaugaMembru(int id, AdaugareMembruDto dto)
    {
        var comisie = await _context.Comisii.FirstOrDefaultAsync(c => c.Id == id);
        if (comisie is null)
            return NotFound("Comisia nu există.");

        var consilier = await _context.Consilieri.FirstOrDefaultAsync(c => c.Id == dto.ConsilierId);
        if (consilier is null)
            return NotFound("Consilierul nu există.");

        // Excepție conștientă de la pattern-ul restore-on-re-add (plan2.md): la ComisieMembru,
        // re-adăugarea creează rând nou pentru a păstra istoricul ca două mandate distincte.
        var areMembrieActiva = await _context.ComisieMembri
            .AnyAsync(m => m.ComisieId == id
                        && m.ConsilierId == dto.ConsilierId
                        && m.DataSfarsit == null);

        if (areMembrieActiva)
            return Conflict("Consilierul este deja membru activ al comisiei.");

        var membru = new ComisieMembru
        {
            ComisieId = id,
            ConsilierId = dto.ConsilierId,
            Rol = dto.Rol,
            DataInceput = dto.DataInceput,
            DataInceputEstimata = false
        };

        _context.ComisieMembri.Add(membru);
        await _context.SaveChangesAsync();

        return Ok(new MembruComisieDto(
            consilier.Id, consilier.NumeComplet, dto.Rol,
            membru.DataInceput, membru.DataSfarsit, membru.DataInceputEstimata));
    }

    [HttpDelete("{id}/Membri/{consilierId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ScoateMembru(
        int id, int consilierId, [FromQuery] DateOnly? dataSfarsit = null)
    {
        // Scoaterea = setare DataSfarsit (închidere normală a membriei), NU soft-delete.
        // Caut DOAR membria activă; cele istorice cu DataSfarsit setat nu se reșterg.
        var membru = await _context.ComisieMembri
            .FirstOrDefaultAsync(m => m.ComisieId == id
                                   && m.ConsilierId == consilierId
                                   && m.DataSfarsit == null);

        if (membru is null) return NotFound();

        var ds = dataSfarsit ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (ds < membru.DataInceput)
            return BadRequest("DataSfarsit nu poate fi anterioară DataInceput a membriei.");

        membru.DataSfarsit = ds;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}