using Cleriq.Data;
using Cleriq.DTOs;
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