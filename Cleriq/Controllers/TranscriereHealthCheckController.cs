using Cleriq.DTOs;
using Cleriq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/Transcriere/HealthCheck")]
[Authorize(Roles = "Admin")]
public class TranscriereHealthCheckController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Verifica(
        [FromServices] IServiceProvider sp,
        CancellationToken ct)
    {
        var serviciu = sp.GetService<IServiciuTranscriere>();
        if (serviciu is null)
            return Ok(new RezultatVerificareTranscriereDto(
                false, null,
                "Whisper:UrlBaza nu este configurat în acest mediu. " +
                "Worker-ul de transcriere nu rulează."));

        var rez = await serviciu.VerificaAsync(ct);
        return Ok(new RezultatVerificareTranscriereDto(
            rez.Succes, rez.LatentaMs, rez.Detalii));
    }
}