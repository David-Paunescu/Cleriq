using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Helpers;
using Cleriq.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("public/{slug}/sedinte/{sedintaId:int}/convocari")]
public class PublicConvocariController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublicConvocariController(AppDbContext context)
    {
        _context = context;
    }

    // GET /public/{slug}/sedinte/{sedintaId}/convocari
    [HttpGet]
    public async Task<IActionResult> Lista(int sedintaId)
    {
        var sedintaPublica = await _context.Sedinte.AnyAsync(s =>
            s.Id == sedintaId
            && s.Status >= StatusSedinta.Convocata
            && s.Status != StatusSedinta.Anulata);
        if (!sedintaPublica) return NotFound();

        var convocari = await _context.Convocari
            .Include(c => c.Consilier)
            .Where(c => c.SedintaId == sedintaId)
            .OrderBy(c => c.Consilier.NumeComplet)
            .ToListAsync();

        var rezultat = convocari
            .Select(c => new PublicConvocareDto(c.Consilier.NumeComplet, c.StatusGeneral()))
            .ToList();

        return Ok(rezultat);
    }
}