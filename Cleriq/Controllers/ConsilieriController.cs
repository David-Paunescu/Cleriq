using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Helpers;
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
            .OrderBy(c => c.NumeComplet)
            .Select(c => new
            {
                Consilier = c,
                AreCont = _context.Users.Any(u => u.ConsilierId == c.Id)
            })
            .ToListAsync();

        return Ok(rezultat.Select(x => MapeazaSpreDto(x.Consilier, x.AreCont)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Detalii(int id)
    {
        var consilier = await _context.Consilieri.FirstOrDefaultAsync(c => c.Id == id);
        if (consilier is null)
            return NotFound();

        var areCont = await _context.Users.AnyAsync(u => u.ConsilierId == id);
        return Ok(MapeazaSpreDto(consilier, areCont));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Creeaza(CreareConsilierDto dto)
    {
        var (telefonNormalizat, eroare) = NormalizeazaTelefon(dto.Telefon);
        if (eroare is not null)
            return BadRequest(eroare);

        var consilier = new Consilier
        {
            NumeComplet = dto.NumeComplet,
            Email = dto.Email,
            Telefon = telefonNormalizat,
            Activ = true
        };

        _context.Consilieri.Add(consilier);
        await _context.SaveChangesAsync();

        return Ok(MapeazaSpreDto(consilier, areCont: false));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Actualizeaza(int id, ActualizareConsilierDto dto)
    {
        var consilier = await _context.Consilieri.FirstOrDefaultAsync(c => c.Id == id);
        if (consilier is null)
            return NotFound();

        var (telefonNormalizat, eroare) = NormalizeazaTelefon(dto.Telefon);
        if (eroare is not null)
            return BadRequest(eroare);

        consilier.NumeComplet = dto.NumeComplet;
        consilier.Email = dto.Email;
        consilier.Telefon = telefonNormalizat;
        consilier.Activ = dto.Activ;

        await _context.SaveChangesAsync();

        var areCont = await _context.Users.AnyAsync(u => u.ConsilierId == id);
        return Ok(MapeazaSpreDto(consilier, areCont));
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

    [HttpPost("{id}/Cont")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreeazaCont(int id, CreareContConsilierDto dto)
    {
        var consilier = await _context.Consilieri.FirstOrDefaultAsync(c => c.Id == id);
        if (consilier is null)
            return NotFound("Consilierul nu există.");

        var areContDeja = await _userManager.Users
            .AnyAsync(u => u.ConsilierId == id);
        if (areContDeja)
            return Conflict("Consilierul are deja un cont de acces.");

        var user = new Utilizator
        {
            UserName = dto.Email,
            Email = dto.Email,
            NumeComplet = consilier.NumeComplet,
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

    private static (string? Normalizat, string? Eroare) NormalizeazaTelefon(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (null, null);

        var normalizat = Telefon.Normalizeaza(input);
        if (normalizat is null)
            return (null, "Format telefon invalid. Folosește format național (0720123456) sau internațional (+40720123456, 0040720123456).");

        return (normalizat, null);
    }

    private static ConsilierDto MapeazaSpreDto(Consilier c, bool areCont) => new(
        c.Id, c.NumeComplet, c.Email, c.Telefon, c.Activ, c.InstitutieId, c.CreatLa, areCont);
}