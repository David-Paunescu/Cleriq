using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Secretar")]
public class RegistruComunicariPrefectController : ControllerBase
{
    private readonly AppDbContext _context;

    public RegistruComunicariPrefectController(AppDbContext context)
    {
        _context = context;
    }

    // Registru cronologic complet (toate comunicările, toate HCL-urile dintr-un an).
    [HttpGet]
    public async Task<IActionResult> Lista(
        [FromQuery] int? an,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50)
    {
        if (page < 1) page = 1;
        if (size <= 0 || size > 200) size = 50;
        var anRegistru = an ?? DateTime.UtcNow.Year;

        var comunicari = await _context.ComunicariHclPrefect
            .Include(c => c.Hcl)
            .Where(c => c.AnRegistru == anRegistru)
            .OrderBy(c => c.NumarOrdineInRegistru)
            .Skip((page - 1) * size).Take(size)
            .ToListAsync();

        var rezultat = comunicari.Select(c => new RegistruComunicareDto(
            c.Id, c.NumarOrdineInRegistru, c.AnRegistru, c.DataTrimiteri,
            c.HclId, FormateazaNumar(c.Hcl), c.Hcl?.Titlu ?? "—",
            c.CanalTransmitere, c.RaspunsPrefect)).ToList();

        return Ok(rezultat);
    }

    private static string? FormateazaNumar(Hcl? h)
        => h?.Numar != null && h.AnNumerotare != null ? $"{h.Numar}/{h.AnNumerotare}" : null;
}