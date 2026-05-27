using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("public/{slug}/sedinte/{sedintaId:int}/voturi")]
public class PublicVoturiController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublicVoturiController(AppDbContext context)
    {
        _context = context;
    }

    // GET /public/{slug}/sedinte/{sedintaId}/voturi
    // Grupate pe punct, ordonate cum apar pe ordinea de zi
    [HttpGet]
    public async Task<IActionResult> Lista(int sedintaId)
    {
        var sedintaPublica = await _context.Sedinte.AnyAsync(s =>
            s.Id == sedintaId
            && s.Status >= StatusSedinta.Convocata
            && s.Status != StatusSedinta.Anulata);
        if (!sedintaPublica) return NotFound();

        var puncte = await _context.PuncteOrdineZi
            .Where(p => p.SedintaId == sedintaId && p.NecesitaVot)
            .Include(p => p.Voturi)
                .ThenInclude(v => v.Consilier)
            .OrderBy(p => p.Ordine)
            .ToListAsync();

        var rezultat = puncte.Select(p => new PublicVoturiPunctDto(
            p.Id, p.Ordine, p.Titlu, p.Rezultat,
            p.Voturi
                .OrderBy(v => v.Consilier.NumeComplet)
                .Select(v => new PublicVotDto(v.Consilier.NumeComplet, v.Optiune))
                .ToList()
        )).ToList();

        return Ok(rezultat);
    }
}