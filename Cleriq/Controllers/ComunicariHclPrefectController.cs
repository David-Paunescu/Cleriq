using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/Hcl/{hclId}/Comunicari")]
[Authorize]
public class ComunicariHclPrefectController : ControllerBase
{
    private const int SqlServerErrorUniqueConstraint = 2601;
    private const int SqlServerErrorDuplicateKey = 2627;

    private readonly AppDbContext _context;
    private readonly IServiciuComunicareHclPrefect _serviciu;

    public ComunicariHclPrefectController(
        AppDbContext context, IServiciuComunicareHclPrefect serviciu)
    {
        _context = context;
        _serviciu = serviciu;
    }

    [HttpGet]
    public async Task<IActionResult> Lista(int hclId)
    {
        var hclExista = await _context.Hcluri.AnyAsync(h => h.Id == hclId);
        if (!hclExista) return NotFound("HCL inexistent.");

        var comunicari = await _context.ComunicariHclPrefect
            .Where(c => c.HclId == hclId)
            .OrderByDescending(c => c.NumarOrdineInRegistru)
            .ToListAsync();

        return Ok(comunicari.Select(MapeazaSpreDto));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Adauga(int hclId, CreareComunicareDto dto)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == hclId);
        if (hcl is null) return NotFound("HCL inexistent.");

        if (hcl.Status < StatusHclRedactional.Numerotat)
            return Conflict("Comunicarea către prefect e posibilă doar după numerotarea HCL (Status >= Numerotat).");

        var anRegistru = dto.DataTrimiteri.Year;

        // Compare-and-swap pe filtered unique (Institutie, AnRegistru, NumarOrdineInRegistru):
        // la coliziune cu o cerere paralelă, recalculăm numărul și reîncercăm.
        const int maxIncercari = 3;
        for (var incercare = 1; incercare <= maxIncercari; incercare++)
        {
            var numarOrdine = await _serviciu.SugereazaNumarOrdineRegistruAsync(
                _context.InstitutieIdCurenta, anRegistru);

            var comunicare = new ComunicareHclPrefect
            {
                HclId = hclId,
                NumarOrdineInRegistru = numarOrdine,
                AnRegistru = anRegistru,
                DataTrimiteri = dto.DataTrimiteri,
                DataInregistrareInRegistru = dto.DataTrimiteri,
                CanalTransmitere = dto.CanalTransmitere,
                NrInregistrarePrefect = NormalizeazaText(dto.NrInregistrarePrefect),
                DataConfirmarePrefect = dto.DataConfirmarePrefect,
                ObservatiiInterne = NormalizeazaText(dto.ObservatiiInterne)
            };

            _context.ComunicariHclPrefect.Add(comunicare);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(MapeazaSpreDto(comunicare));
            }
            catch (DbUpdateException ex) when (EsteViolareUnique(ex))
            {
                _context.Entry(comunicare).State = EntityState.Detached;
                if (incercare == maxIncercari)
                    return Conflict("Nu s-a putut atribui un număr de ordine în registru (conflict de concurență). Reîncearcă.");
            }
        }

        return Conflict("Nu s-a putut atribui un număr de ordine în registru.");
    }

    [HttpPut("{comunicareId}")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Actualizeaza(
        int hclId, int comunicareId, ActualizareComunicareDto dto)
    {
        var comunicare = await _context.ComunicariHclPrefect
            .FirstOrDefaultAsync(c => c.Id == comunicareId && c.HclId == hclId);
        if (comunicare is null) return NotFound("Comunicarea nu există pe acest HCL.");

        // Imutabile post-creare: HclId, NumarOrdineInRegistru, AnRegistru, DataTrimiteri, CanalTransmitere
        comunicare.RaspunsPrefect = dto.Raspuns;
        comunicare.DataRaspunsPrefect = dto.DataRaspuns;
        comunicare.ObiectiiMotivate = NormalizeazaText(dto.ObiectiiMotivate);
        comunicare.ObservatiiInterne = NormalizeazaText(dto.ObservatiiInterne);
        comunicare.NrInregistrarePrefect = NormalizeazaText(dto.NrInregistrarePrefect);
        comunicare.DataConfirmarePrefect = dto.DataConfirmarePrefect;

        await _context.SaveChangesAsync();
        return Ok(MapeazaSpreDto(comunicare));
    }

    [HttpDelete("{comunicareId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Sterge(int hclId, int comunicareId)
    {
        var comunicare = await _context.ComunicariHclPrefect
            .FirstOrDefaultAsync(c => c.Id == comunicareId && c.HclId == hclId);
        if (comunicare is null) return NotFound("Comunicarea nu există pe acest HCL.");

        _context.ComunicariHclPrefect.Remove(comunicare);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static bool EsteViolareUnique(DbUpdateException ex)
        => ex.InnerException is SqlException sql
           && (sql.Number == SqlServerErrorUniqueConstraint
               || sql.Number == SqlServerErrorDuplicateKey);

    private static string? NormalizeazaText(string? input)
        => string.IsNullOrWhiteSpace(input) ? null : input.Trim();

    private static ComunicareHclPrefectDto MapeazaSpreDto(ComunicareHclPrefect c) => new(
        c.Id, c.HclId, c.NumarOrdineInRegistru, c.AnRegistru,
        c.DataTrimiteri, c.DataInregistrareInRegistru, c.CanalTransmitere,
        c.NrInregistrarePrefect, c.DataConfirmarePrefect, c.ObiectiiMotivate,
        c.RaspunsPrefect, c.DataRaspunsPrefect, c.ObservatiiInterne, c.CreatLa);
}