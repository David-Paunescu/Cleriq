using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConsilieriController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<Utilizator> _userManager;

    public ConsilieriController(AppDbContext context, UserManager<Utilizator> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Lista()
    {
        var rezultat = await _context.Consilieri
            .Select(c => MapeazaSpreDto(c))
            .ToListAsync();

        return Ok(rezultat);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalii(int id)
    {
        var consilier = await _context.Consilieri.FirstOrDefaultAsync(c => c.Id == id);
        if (consilier is null)
            return NotFound();

        return Ok(MapeazaSpreDto(consilier));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Creeaza(CreareConsilierDto dto)
    {
        var consilier = new Consilier
        {
            NumeComplet = dto.NumeComplet,
            Email = dto.Email,
            Telefon = dto.Telefon,
            Activ = true
            // InstitutieId se setează automat în SaveChanges din tokenul Adminului
        };

        _context.Consilieri.Add(consilier);
        await _context.SaveChangesAsync();

        return Ok(MapeazaSpreDto(consilier));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Actualizeaza(int id, ActualizareConsilierDto dto)
    {
        var consilier = await _context.Consilieri.FirstOrDefaultAsync(c => c.Id == id);
        if (consilier is null)
            return NotFound();

        consilier.NumeComplet = dto.NumeComplet;
        consilier.Email = dto.Email;
        consilier.Telefon = dto.Telefon;
        consilier.Activ = dto.Activ;

        await _context.SaveChangesAsync();

        return Ok(MapeazaSpreDto(consilier));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Sterge(int id)
    {
        var consilier = await _context.Consilieri.FirstOrDefaultAsync(c => c.Id == id);
        if (consilier is null)
            return NotFound();

        _context.Consilieri.Remove(consilier); // devine soft-delete în SaveChanges
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // === Cont de acces pentru consilier (vot individual) ===

    [HttpPost("{id}/Cont")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreeazaCont(int id, CreareContConsilierDto dto)
    {
        // Consilierul trebuie să existe în tenant-ul curent (filtrul global aplică).
        var consilier = await _context.Consilieri.FirstOrDefaultAsync(c => c.Id == id);
        if (consilier is null)
            return NotFound("Consilierul nu există.");

        // Un singur cont per consilier (verificare explicită → mesaj clar;
        // indexul unic filtrat e plasa de siguranță la nivel DB).
        var areContDeja = await _userManager.Users
            .AnyAsync(u => u.ConsilierId == id);
        if (areContDeja)
            return Conflict("Consilierul are deja un cont de acces.");

        var user = new Utilizator
        {
            UserName = dto.Email,
            Email = dto.Email,
            NumeComplet = consilier.NumeComplet,   // numele vine de la consilier, sursă unică
            InstitutieId = _context.InstitutieIdCurenta,
            ConsilierId = id,
            EmailConfirmed = true
        };

        var rezultat = await _userManager.CreateAsync(user, dto.Parola);
        if (!rezultat.Succeeded)
            return BadRequest(rezultat.Errors);

        await _userManager.AddToRoleAsync(user, "Consilier");

        return Ok(new ContConsilierDto(user.Id, user.Email!, id));
    }

    private static ConsilierDto MapeazaSpreDto(Consilier c) => new(
        c.Id, c.NumeComplet, c.Email, c.Telefon, c.Activ, c.InstitutieId, c.CreatLa);
}