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
    [Authorize(Roles = "Admin,Secretar")]
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

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalii(int id)
    {
        var sedinta = await _context.Sedinte.FirstOrDefaultAsync(s => s.Id == id);
        if (sedinta is null)
            return NotFound();

        return Ok(MapeazaSpreDto(sedinta));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Sterge(int id)
    {
        var sedinta = await _context.Sedinte.FirstOrDefaultAsync(s => s.Id == id);
        if (sedinta is null)
            return NotFound();

        _context.Sedinte.Remove(sedinta); // devine soft-delete în SaveChanges
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Actualizeaza(int id, ActualizareSedintaDto dto)
    {
        var sedinta = await _context.Sedinte.FirstOrDefaultAsync(s => s.Id == id);
        if (sedinta is null)
            return NotFound("Ședința nu există.");

        if (sedinta.Status != StatusSedinta.Planificata
            && sedinta.Status != StatusSedinta.Convocata)
            return Conflict($"Nu se poate edita o ședință cu status {sedinta.Status}.");

        sedinta.Titlu = dto.Titlu;
        sedinta.Numar = dto.Numar;
        sedinta.Tip = dto.Tip;
        sedinta.DataOra = dto.DataOra;
        sedinta.Loc = dto.Loc;
        sedinta.ModDesfasurare = dto.ModDesfasurare;

        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(sedinta));
    }

    // === Tranziții de status (RPC-style, ca la Puncte/{id}/Inchide) ===

    [HttpPost("{id}/Incepe")]
    [Authorize(Roles = "Admin,Secretar")]
    public Task<IActionResult> Incepe(int id)
        => SchimbaStatus(id, StatusSedinta.InDesfasurare);

    [HttpPost("{id}/Finalizeaza")]
    [Authorize(Roles = "Admin,Secretar")]
    public Task<IActionResult> Finalizeaza(int id)
        => SchimbaStatus(id, StatusSedinta.Finalizata);

    [HttpPost("{id}/Anuleaza")]
    [Authorize(Roles = "Admin,Secretar")]
    public Task<IActionResult> Anuleaza(int id)
        => SchimbaStatus(id, StatusSedinta.Anulata);

    private async Task<IActionResult> SchimbaStatus(int id, StatusSedinta nou)
    {
        var sedinta = await _context.Sedinte.FirstOrDefaultAsync(s => s.Id == id);
        if (sedinta is null)
            return NotFound("Ședința nu există.");

        var eroare = ValideazaTranzitie(sedinta.Status, nou);
        if (eroare is not null)
            return Conflict(eroare);

        sedinta.Status = nou;
        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(sedinta));
    }

    // Matricea tranzițiilor permise manual prin acest controller.
    // Planificata → Convocata se face prin ConvocareController, nu aici.
    private static string? ValideazaTranzitie(StatusSedinta curent, StatusSedinta nou)
    {
        var permis = (curent, nou) switch
        {
            (StatusSedinta.Convocata, StatusSedinta.InDesfasurare) => true,
            (StatusSedinta.InDesfasurare, StatusSedinta.Finalizata) => true,
            (StatusSedinta.Planificata, StatusSedinta.Anulata) => true,
            (StatusSedinta.Convocata, StatusSedinta.Anulata) => true,
            _ => false
        };

        return permis
            ? null
            : $"Tranziție invalidă: nu se poate trece din {curent} în {nou}.";
    }

    private static SedintaDto MapeazaSpreDto(Sedinta s) => new(
        s.Id, s.Titlu, s.Numar, s.Tip, s.DataOra,
        s.Loc, s.ModDesfasurare, s.Status, s.InstitutieId, s.CreatLa);
}