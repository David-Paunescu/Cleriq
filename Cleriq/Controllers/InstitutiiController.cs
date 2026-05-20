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
public class InstitutiiController : ControllerBase
{
    private readonly AppDbContext _context;

    public InstitutiiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Creeaza(CreareInstitutieDto dto)
    {
        var institutie = new Institutie
        {
            Denumire = dto.Denumire,
            Judet = dto.Judet,
            CodSiruta = dto.CodSiruta,
            Tip = dto.Tip
        };

        _context.Institutii.Add(institutie);
        await _context.SaveChangesAsync();

        return Ok(new InstitutieDto(
            institutie.Id, institutie.Denumire, institutie.Judet, institutie.CodSiruta,
            institutie.Tip, institutie.StatusAbonament, institutie.CreatLa));
    }

    [HttpGet]
    public async Task<IActionResult> Lista()
    {
        var rezultat = await _context.Institutii
            .Select(i => new InstitutieDto(
                i.Id, i.Denumire, i.Judet, i.CodSiruta,
                i.Tip, i.StatusAbonament, i.CreatLa))
            .ToListAsync();

        return Ok(rezultat);
    }
}