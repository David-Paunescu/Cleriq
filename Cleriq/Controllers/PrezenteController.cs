using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/Sedinte/{sedintaId}")]
[Authorize]
public class PrezenteController : ControllerBase
{
    private readonly AppDbContext _context;

    public PrezenteController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("Prezente")]
    public async Task<IActionResult> ListaPrezente(int sedintaId)
    {
        var sedinta = await _context.Sedinte.FirstOrDefaultAsync(s => s.Id == sedintaId);
        if (sedinta is null)
            return NotFound("Ședința nu există.");

        var consilieri = await _context.Consilieri
            .Where(c => c.Activ)
            .OrderBy(c => c.NumeComplet)
            .ToListAsync();

        var prezente = await _context.Prezente
            .Where(p => p.SedintaId == sedintaId)
            .ToDictionaryAsync(p => p.ConsilierId);

        var rezultat = consilieri.Select(c =>
            prezente.TryGetValue(c.Id, out var p)
                ? new PrezentaDto(c.Id, c.NumeComplet, p.Status, p.OraSosire)
                : new PrezentaDto(c.Id, c.NumeComplet, StatusPrezenta.Absent, null)
        ).ToList();

        return Ok(rezultat);
    }

    [HttpPost("Prezente")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> SeteazaPrezenta(int sedintaId, SetarePrezentaDto dto)
    {
        var sedinta = await _context.Sedinte.FirstOrDefaultAsync(s => s.Id == sedintaId);
        if (sedinta is null)
            return NotFound("Ședința nu există.");

        var consilier = await _context.Consilieri
            .FirstOrDefaultAsync(c => c.Id == dto.ConsilierId);
        if (consilier is null)
            return NotFound("Consilierul nu există.");

        // Caută și printre cei soft-deleted (din același tenant) ca să restaurăm/actualizăm
        var existent = await _context.Prezente
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.SedintaId == sedintaId
                                   && p.ConsilierId == dto.ConsilierId
                                   && p.InstitutieId == _context.InstitutieIdCurenta);

        if (existent is null)
        {
            _context.Prezente.Add(new Prezenta
            {
                SedintaId = sedintaId,
                ConsilierId = dto.ConsilierId,
                Status = dto.Status,
                OraSosire = dto.OraSosire
            });
        }
        else
        {
            existent.Status = dto.Status;
            existent.OraSosire = dto.OraSosire;
            if (existent.EsteSters)
            {
                existent.EsteSters = false;
                existent.StersLa = null;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new PrezentaDto(consilier.Id, consilier.NumeComplet, dto.Status, dto.OraSosire));
    }

    [HttpGet("Cvorum")]
    public async Task<IActionResult> Cvorum(int sedintaId)
    {
        var sedinta = await _context.Sedinte.FirstOrDefaultAsync(s => s.Id == sedintaId);
        if (sedinta is null)
            return NotFound("Ședința nu există.");

        var totalActivi = await _context.Consilieri.CountAsync(c => c.Activ);

        var prezenti = await _context.Prezente
            .CountAsync(p => p.SedintaId == sedintaId
                          && (p.Status == StatusPrezenta.Prezent
                              || p.Status == StatusPrezenta.OnlinePrezent));

        var cvorumNecesar = (totalActivi / 2) + 1;

        return Ok(new CvorumDto(
            TotalConsilieriActivi: totalActivi,
            Prezenti: prezenti,
            CvorumNecesar: cvorumNecesar,
            CvorumIntrunit: prezenti >= cvorumNecesar));
    }
}