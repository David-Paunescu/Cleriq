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
public class MandateController : ControllerBase
{
    private readonly AppDbContext _context;

    public MandateController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Lista([FromQuery] int? consilierId = null)
    {
        var query = _context.Mandate.Include(m => m.Consilier).AsQueryable();
        if (consilierId.HasValue)
            query = query.Where(m => m.ConsilierId == consilierId.Value);

        var rezultat = await query
            .Select(m => new MandatDto(
                m.Id, m.ConsilierId, m.Consilier.NumeComplet,
                m.DataInceput, m.DataSfarsit, m.GrupPolitic,
                m.InstitutieId, m.CreatLa))
            .ToListAsync();

        return Ok(rezultat);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalii(int id)
    {
        var mandat = await _context.Mandate
            .Include(m => m.Consilier)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mandat is null)
            return NotFound();

        return Ok(MapeazaSpreDto(mandat));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Creeaza(CreareMandatDto dto)
    {
        var consilier = await _context.Consilieri
            .FirstOrDefaultAsync(c => c.Id == dto.ConsilierId);
        if (consilier is null)
            return NotFound("Consilierul nu există.");

        if (dto.DataSfarsit.HasValue && dto.DataSfarsit.Value < dto.DataInceput)
            return BadRequest("Data de sfârșit nu poate fi înaintea datei de început.");

        var mandat = new Mandat
        {
            ConsilierId = dto.ConsilierId,
            DataInceput = dto.DataInceput,
            DataSfarsit = dto.DataSfarsit,
            GrupPolitic = dto.GrupPolitic,
            Consilier = consilier // ca să nu refacem query pentru numeComplet
        };

        _context.Mandate.Add(mandat);
        await _context.SaveChangesAsync();

        return Ok(MapeazaSpreDto(mandat));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Actualizeaza(int id, ActualizareMandatDto dto)
    {
        var mandat = await _context.Mandate
            .Include(m => m.Consilier)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (mandat is null)
            return NotFound();

        if (dto.DataSfarsit.HasValue && dto.DataSfarsit.Value < dto.DataInceput)
            return BadRequest("Data de sfârșit nu poate fi înaintea datei de început.");

        mandat.DataInceput = dto.DataInceput;
        mandat.DataSfarsit = dto.DataSfarsit;
        mandat.GrupPolitic = dto.GrupPolitic;

        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(mandat));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Sterge(int id)
    {
        var mandat = await _context.Mandate.FirstOrDefaultAsync(m => m.Id == id);
        if (mandat is null)
            return NotFound();

        _context.Mandate.Remove(mandat);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static MandatDto MapeazaSpreDto(Mandat m) => new(
        m.Id, m.ConsilierId, m.Consilier.NumeComplet,
        m.DataInceput, m.DataSfarsit, m.GrupPolitic,
        m.InstitutieId, m.CreatLa);
}