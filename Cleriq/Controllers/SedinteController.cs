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
public class SedinteController : ControllerBase
{
    private readonly AppDbContext _context;

    public SedinteController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Creeaza(CreareSedintaDto dto)
    {
        var sedinta = new Sedinta
        {
            Titlu = dto.Titlu,
            Numar = dto.Numar,
            Tip = dto.Tip,
            DataOra = dto.DataOra,
            Loc = dto.Loc,
            ModDesfasurare = dto.ModDesfasurare
            // InstitutieId NU se setează aici — se pune automat în SaveChanges
        };

        _context.Sedinte.Add(sedinta);
        await _context.SaveChangesAsync();

        return Ok(MapeazaSpreDto(sedinta));
    }

    [HttpGet]
    public async Task<IActionResult> Lista()
    {
        var rezultat = await _context.Sedinte
            .Select(s => MapeazaSpreDto(s))
            .ToListAsync();

        return Ok(rezultat);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Sterge(int id)
    {
        var sedinta = await _context.Sedinte.FirstOrDefaultAsync(s => s.Id == id);
        if (sedinta is null)
            return NotFound();

        _context.Sedinte.Remove(sedinta); // devine soft-delete în SaveChanges
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static SedintaDto MapeazaSpreDto(Sedinta s) => new(
        s.Id, s.Titlu, s.Numar, s.Tip, s.DataOra,
        s.Loc, s.ModDesfasurare, s.Status, s.InstitutieId, s.CreatLa);
}