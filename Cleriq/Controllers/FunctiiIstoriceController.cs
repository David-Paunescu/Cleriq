using Cleriq.DTOs;
using Cleriq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Secretar")]
public class FunctiiIstoriceController : ControllerBase
{
    private readonly IServiciuFunctiiIstorice _serviciu;

    public FunctiiIstoriceController(IServiciuFunctiiIstorice serviciu)
    {
        _serviciu = serviciu;
    }

    [HttpGet("Primar")]
    public async Task<IActionResult> Primar([FromQuery] DateOnly data, CancellationToken ct)
    {
        var p = await _serviciu.CinEPrimarulLa(data, ct);
        return new JsonResult(p is null ? null : new SubiectIstoricDto(p.Id, p.NumeComplet));
    }

    [HttpGet("SecretarUat")]
    public async Task<IActionResult> SecretarUat([FromQuery] DateOnly data, CancellationToken ct)
    {
        var p = await _serviciu.CinESecretarulUatLa(data, ct);
        return new JsonResult(p is null ? null : new SubiectIstoricDto(p.Id, p.NumeComplet));
    }

    [HttpGet("Viceprimari")]
    public async Task<IActionResult> Viceprimari([FromQuery] DateOnly data, CancellationToken ct)
    {
        var lista = await _serviciu.CineEViceprimariiLa(data, ct);
        return Ok(lista.Select(t => new ViceprimarIstoricDto(
            t.Consilier.Id, t.Consilier.NumeComplet,
            t.Mandat.Id, t.Mandat.DataInceput, t.Mandat.DataSfarsit)).ToList());
    }

    [HttpGet("Consilieri")]
    public async Task<IActionResult> Consilieri([FromQuery] DateOnly data, CancellationToken ct)
    {
        var lista = await _serviciu.CineEConsilieriiLa(data, ct);
        return Ok(lista.Select(c => new SubiectIstoricDto(c.Id, c.NumeComplet)).ToList());
    }

    [HttpGet("Comisii/{comisieId}/Membri")]
    public async Task<IActionResult> MembriComisie(
        int comisieId, [FromQuery] DateOnly data, CancellationToken ct)
    {
        var lista = await _serviciu.CineEMembriiComisieiLa(comisieId, data, ct);
        return Ok(lista.Select(t => new MembruIstoricDto(
            t.Consilier.Id, t.Consilier.NumeComplet, t.Rol)).ToList());
    }

    [HttpGet("Comisii/{comisieId}/Presedinte")]
    public async Task<IActionResult> PresedinteComisie(
        int comisieId, [FromQuery] DateOnly data, CancellationToken ct)
    {
        var c = await _serviciu.CinePresedinteleComisieiLa(comisieId, data, ct);
        return new JsonResult(c is null ? null : new SubiectIstoricDto(c.Id, c.NumeComplet));
    }
}