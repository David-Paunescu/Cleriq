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
[Route("api/Dispozitii/{dispozitieId}/Comunicari")]
[Authorize]
public class ComunicariDispozitiiPrefectController : ControllerBase
{
    private const int SqlServerErrorUniqueConstraint = 2601;
    private const int SqlServerErrorDuplicateKey = 2627;

    private readonly AppDbContext _context;
    private readonly IServiciuComunicareDispozitiePrefect _serviciu;

    public ComunicariDispozitiiPrefectController(
        AppDbContext context, IServiciuComunicareDispozitiePrefect serviciu)
    {
        _context = context;
        _serviciu = serviciu;
    }

    [HttpGet]
    public async Task<IActionResult> Lista(int dispozitieId)
    {
        var exista = await _context.Dispozitii.AnyAsync(d => d.Id == dispozitieId);
        if (!exista) return NotFound("Dispoziție inexistentă.");

        var comunicari = await _context.ComunicariDispozitiePrefect
            .Where(c => c.DispozitieId == dispozitieId)
            .OrderByDescending(c => c.NumarOrdineInRegistru)
            .ToListAsync();

        return Ok(comunicari.Select(MapeazaSpreDto));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Adauga(int dispozitieId, CreareComunicareDto dto)
    {
        var dispozitie = await _context.Dispozitii.FirstOrDefaultAsync(d => d.Id == dispozitieId);
        if (dispozitie is null) return NotFound("Dispoziție inexistentă.");

        if (dispozitie.Status < StatusActRedactional.Numerotat)
            return Conflict("Comunicarea către prefect e posibilă doar după numerotarea dispoziției (Status >= Numerotat).");

        // Comunicarea la prefect = intrare în circuitul de control de legalitate → îngheață definitiv
        // varianta semnată (se persistă odată cu comunicarea, dispozitie fiind tracked).
        dispozitie.AIntratInCircuit = true;

        var anRegistru = dto.DataTrimiteri.Year;

        // Compare-and-swap pe filtered unique (Institutie, AnRegistru, NumarOrdineInRegistru):
        // la coliziune cu o cerere paralelă, recalculăm numărul și reîncercăm.
        const int maxIncercari = 3;
        for (var incercare = 1; incercare <= maxIncercari; incercare++)
        {
            var numarOrdine = await _serviciu.SugereazaNumarOrdineRegistruAsync(
                _context.InstitutieIdCurenta, anRegistru);

            var comunicare = new ComunicareDispozitiePrefect
            {
                DispozitieId = dispozitieId,
                NumarOrdineInRegistru = numarOrdine,
                AnRegistru = anRegistru,
                DataTrimiteri = dto.DataTrimiteri,
                DataInregistrareInRegistru = dto.DataTrimiteri,
                CanalTransmitere = dto.CanalTransmitere,
                NrInregistrarePrefect = NormalizeazaText(dto.NrInregistrarePrefect),
                DataConfirmarePrefect = dto.DataConfirmarePrefect,
                ObservatiiInterne = NormalizeazaText(dto.ObservatiiInterne)
            };

            _context.ComunicariDispozitiePrefect.Add(comunicare);

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
        int dispozitieId, int comunicareId, ActualizareComunicareDto dto)
    {
        var comunicare = await _context.ComunicariDispozitiePrefect
            .FirstOrDefaultAsync(c => c.Id == comunicareId && c.DispozitieId == dispozitieId);
        if (comunicare is null) return NotFound("Comunicarea nu există pe această dispoziție.");

        // Imutabile post-creare: DispozitieId, NumarOrdineInRegistru, AnRegistru, DataTrimiteri, CanalTransmitere
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
    public async Task<IActionResult> Sterge(int dispozitieId, int comunicareId)
    {
        var comunicare = await _context.ComunicariDispozitiePrefect
            .FirstOrDefaultAsync(c => c.Id == comunicareId && c.DispozitieId == dispozitieId);
        if (comunicare is null) return NotFound("Comunicarea nu există pe această dispoziție.");

        _context.ComunicariDispozitiePrefect.Remove(comunicare);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static bool EsteViolareUnique(DbUpdateException ex)
        => ex.InnerException is SqlException sql
           && (sql.Number == SqlServerErrorUniqueConstraint
               || sql.Number == SqlServerErrorDuplicateKey);

    private static string? NormalizeazaText(string? input)
        => string.IsNullOrWhiteSpace(input) ? null : input.Trim();

    private static ComunicareDispozitiePrefectDto MapeazaSpreDto(ComunicareDispozitiePrefect c) => new(
        c.Id, c.DispozitieId, c.NumarOrdineInRegistru, c.AnRegistru,
        c.DataTrimiteri, c.DataInregistrareInRegistru, c.CanalTransmitere,
        c.NrInregistrarePrefect, c.DataConfirmarePrefect, c.ObiectiiMotivate,
        c.RaspunsPrefect, c.DataRaspunsPrefect, c.ObservatiiInterne, c.CreatLa);
}
