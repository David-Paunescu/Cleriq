using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/Institutii/Smtp")]
[Authorize(Roles = "Admin")]
public class SmtpController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICriptareSecreta _criptare;
    private readonly IServiciuNotificare _notificare;

    public SmtpController(
        AppDbContext context,
        ICriptareSecreta criptare,
        IServiciuNotificare notificare)
    {
        _context = context;
        _criptare = criptare;
        _notificare = notificare;
    }

    [HttpGet]
    public async Task<IActionResult> Obtine()
    {
        var inst = await _context.Institutii.FirstOrDefaultAsync();
        if (inst is null) return NotFound();

        return Ok(new ConfigSmtpDto(
            inst.SmtpHost,
            inst.SmtpPort,
            inst.SmtpUtilizator,
            inst.SmtpEmailFrom,
            inst.SmtpNumeFrom,
            inst.SmtpSecuritate,
            !string.IsNullOrEmpty(inst.SmtpParolaCriptata)));
    }

    [HttpPut]
    public async Task<IActionResult> Seteaza(SetareSmtpDto dto)
    {
        var inst = await _context.Institutii.FirstOrDefaultAsync();
        if (inst is null) return NotFound();

        var eroare = Valideaza(dto, inst.SmtpParolaCriptata);
        if (eroare is not null) return BadRequest(eroare);

        inst.SmtpHost = dto.Host.Trim();
        inst.SmtpPort = dto.Port;
        inst.SmtpUtilizator = dto.Utilizator.Trim();
        inst.SmtpEmailFrom = dto.EmailFrom.Trim();
        inst.SmtpNumeFrom = string.IsNullOrWhiteSpace(dto.NumeFrom) ? null : dto.NumeFrom.Trim();
        inst.SmtpSecuritate = dto.Securitate;

        if (!string.IsNullOrEmpty(dto.Parola))
            inst.SmtpParolaCriptata = _criptare.Cripteaza(dto.Parola);

        await _context.SaveChangesAsync();

        return Ok(new ConfigSmtpDto(
            inst.SmtpHost, inst.SmtpPort, inst.SmtpUtilizator,
            inst.SmtpEmailFrom, inst.SmtpNumeFrom, inst.SmtpSecuritate,
            !string.IsNullOrEmpty(inst.SmtpParolaCriptata)));
    }

    [HttpPost("Test")]
    public async Task<IActionResult> Test(TestareSmtpDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.EmailDestinatar))
            return Ok(new RezultatTestareSmtpDto(false, "Email destinatar lipsă."));

        var institutieId = _context.InstitutieIdCurenta;

        try
        {
            await using var conexiune = await _notificare.DeschideConexiuneEmailAsync(institutieId, ct);
            var rez = await conexiune.TrimiteAsync(
                dto.EmailDestinatar.Trim(),
                "Test SMTP — Cleriq",
                "<p>Acesta este un email de test trimis din aplicația Cleriq pentru verificarea configurării SMTP.</p>",
                ct);
            return Ok(new RezultatTestareSmtpDto(rez.Succes, rez.Detalii));
        }
        catch (Exception ex)
        {
            return Ok(new RezultatTestareSmtpDto(false, $"{ex.GetType().Name}: {ex.Message}"));
        }
    }

    private static string? Valideaza(SetareSmtpDto dto, string? parolaExistenta)
    {
        if (string.IsNullOrWhiteSpace(dto.Host)) return "Host obligatoriu.";
        if (dto.Port < 1 || dto.Port > 65535) return "Port invalid.";
        if (string.IsNullOrWhiteSpace(dto.Utilizator)) return "Utilizator obligatoriu.";
        if (string.IsNullOrWhiteSpace(dto.EmailFrom)) return "EmailFrom obligatoriu.";
        if (string.IsNullOrEmpty(dto.Parola) && string.IsNullOrEmpty(parolaExistenta))
            return "Parola este obligatorie la prima setare.";
        return null;
    }
}