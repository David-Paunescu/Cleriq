using Cleriq.Data;
using Cleriq.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

// Tenant rezolvat de SlugTenantMiddleware → filtrul global aplică Id == InstitutieIdCurenta
[ApiController]
[Route("public/{slug}")]
public class PublicInstitutieController : ControllerBase
{
    private readonly AppDbContext _context;

    public PublicInstitutieController(AppDbContext context)
    {
        _context = context;
    }

    // GET /public/{slug}
    [HttpGet]
    public async Task<IActionResult> Detalii()
    {
        var institutie = await _context.Institutii.FirstOrDefaultAsync();
        if (institutie is null)
            return NotFound();

        return Ok(new PublicInstitutieDto(
            institutie.Slug,
            institutie.Denumire,
            institutie.Judet,
            institutie.Tip,
            institutie.FusOrar));
    }
}