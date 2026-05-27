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
        var query = _context.Institutii.AsQueryable();

        // SuperAdmin vede toate instituțiile (by-pass filtru global tenant).
        // Soft-delete rămâne aplicat — filtrul global îl combină cu cel de tenant,
        // deci IgnoreQueryFilters() le ridică pe ambele; reaplicăm manual !EsteSters.
        if (User.IsInRole("SuperAdmin"))
        {
            query = query.IgnoreQueryFilters().Where(i => !i.EsteSters);
        }

        var rezultat = await query
            .OrderBy(i => i.Denumire)
            .Select(i => new InstitutieDto(
                i.Id, i.Denumire, i.Slug, i.Judet, i.CodSiruta,
                i.Tip, i.StatusAbonament, i.FusOrar, i.CreatLa))
            .ToListAsync();

        return Ok(rezultat);
    }
}