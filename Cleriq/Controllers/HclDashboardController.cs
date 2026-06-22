using Cleriq.Data;
using Cleriq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/Hcl")]
[Authorize(Roles = "Admin,Secretar")]
public class HclDashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IServiciuComunicareHclPrefect _serviciu;

    public HclDashboardController(AppDbContext context, IServiciuComunicareHclPrefect serviciu)
    {
        _context = context;
        _serviciu = serviciu;
    }

    // GET /api/Hcl/UrgentDeComunicat?prag=3
    // „UrgentDeComunicat" e segment literal → bate ruta {id} din HclController (precedență rutare).
    [HttpGet("UrgentDeComunicat")]
    public async Task<IActionResult> UrgentDeComunicat([FromQuery] int prag = 3)
    {
        var rezultat = await _serviciu.ObtineHclUrgentDeComunicatAsync(
            _context.InstitutieIdCurenta, prag);
        return Ok(rezultat);
    }
}