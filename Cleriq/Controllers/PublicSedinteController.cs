using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

// Tenant-ul e rezolvat de SlugTenantMiddleware înainte de execuția acțiunii.
// Filtrul global tenant + soft-delete se aplică automat din InstitutieIdCurenta
// (citit dinamic prin computed property). Nu există [Authorize] — endpoint-uri publice.
[ApiController]
[Route("public/{slug}/sedinte")]
public class PublicSedinteController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublicSedinteController(AppDbContext context)
    {
        _context = context;
    }

    // GET /public/{slug}/sedinte
    // Lista ședințelor publicate (Status >= Convocata, conform Legii 52/2003).
    [HttpGet]
    public async Task<IActionResult> Lista()
    {
        var sedinte = await _context.Sedinte
            .Where(s => s.Status >= StatusSedinta.Convocata
                     && s.Status != StatusSedinta.Anulata)
            .OrderByDescending(s => s.DataOra)
            .Select(s => new PublicSedintaDto(
                s.Id, s.Titlu, s.Numar, s.Tip,
                s.DataOra, s.Loc, s.ModDesfasurare, s.Status,
                null))                      // lista nu include ordine de zi
            .ToListAsync();

        return Ok(sedinte);
    }

    // GET /public/{slug}/sedinte/{id}
    // Detalii ședință + ordine de zi
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalii(int id)
    {
        var sedinta = await _context.Sedinte
            .Include(s => s.Puncte.OrderBy(p => p.Ordine))
            .Where(s => s.Id == id
                     && s.Status >= StatusSedinta.Convocata
                     && s.Status != StatusSedinta.Anulata)
            .FirstOrDefaultAsync();

        if (sedinta is null)
            return NotFound();

        var ordineDeZi = sedinta.Puncte
            .Select(p => new PublicPunctOrdineZiDto(
                p.Id, p.Ordine, p.Titlu, p.Descriere, p.Tip,
                p.NecesitaVot, p.TipVot, p.TipMajoritate, p.Rezultat))
            .ToList();

        return Ok(new PublicSedintaDto(
            sedinta.Id, sedinta.Titlu, sedinta.Numar, sedinta.Tip,
            sedinta.DataOra, sedinta.Loc, sedinta.ModDesfasurare, sedinta.Status,
            ordineDeZi));
    }
}