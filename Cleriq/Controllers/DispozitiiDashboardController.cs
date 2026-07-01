using Cleriq.Data;
using Cleriq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/Dispozitii")]
[Authorize(Roles = "Admin,Secretar")]
public class DispozitiiDashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IServiciuComunicareDispozitiePrefect _serviciu;

    public DispozitiiDashboardController(
        AppDbContext context, IServiciuComunicareDispozitiePrefect serviciu)
    {
        _context = context;
        _serviciu = serviciu;
    }

    // GET /api/Dispozitii/UrgentDeComunicat?prag=3
    // „UrgentDeComunicat" e segment literal → bate ruta {id} din DispozitiiController (precedență rutare).
    [HttpGet("UrgentDeComunicat")]
    public async Task<IActionResult> UrgentDeComunicat([FromQuery] int prag = 3)
    {
        var rezultat = await _serviciu.ObtineDispozitiiUrgentDeComunicatAsync(
            _context.InstitutieIdCurenta, prag);
        return Ok(rezultat);
    }
}
