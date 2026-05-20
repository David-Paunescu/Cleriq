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
    public async Task<IActionResult> Detalii(int id)
    {
        var comisie = await _context.Comisii
            .Include(c => c.Membri)
                .ThenInclude(m => m.Consilier)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comisie is null)
            return NotFound();

        var membri = comisie.Membri
            .Where(m => m.Consilier is not null) // defensiv: ignoră membri al căror consilier e soft-deleted
            .Select(m => new MembruComisieDto(m.ConsilierId, m.Consilier!.NumeComplet, m.Rol))
            .ToList();

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

        // Caută și printre cei soft-deleted din același tenant, ca să restaurăm dacă găsim
        var existent = await _context.ComisieMembri
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.ComisieId == id
                                   && m.ConsilierId == dto.ConsilierId
                                   && m.InstitutieId == _context.InstitutieIdCurenta);

        if (existent is not null && !existent.EsteSters)
            return Conflict("Consilierul este deja membru al comisiei.");

        if (existent is not null && existent.EsteSters)
        {
            // Restaurăm vechea apartenență
            existent.EsteSters = false;
            existent.StersLa = null;
            existent.Rol = dto.Rol;
        }
        else
        {
            _context.ComisieMembri.Add(new ComisieMembru
            {
                ComisieId = id,
                ConsilierId = dto.ConsilierId,
                Rol = dto.Rol
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new MembruComisieDto(consilier.Id, consilier.NumeComplet, dto.Rol));
    }

    [HttpDelete("{id}/Membri/{consilierId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ScoateMembru(int id, int consilierId)
    {
        var membru = await _context.ComisieMembri
            .FirstOrDefaultAsync(m => m.ComisieId == id && m.ConsilierId == consilierId);

        if (membru is null)
            return NotFound();

        _context.ComisieMembri.Remove(membru);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}