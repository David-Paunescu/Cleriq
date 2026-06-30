using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Helpers;
using Cleriq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/Hcl/{hclId}/Semnatari")]
[Authorize]
public class SemnatariHclController : ControllerBase
{
    private readonly AppDbContext _context;

    public SemnatariHclController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Lista(int hclId)
    {
        var hclExista = await _context.Hcluri.AnyAsync(h => h.Id == hclId);
        if (!hclExista) return NotFound("HCL inexistent.");

        var semnatari = await _context.SemnatariHcl
            .Include(s => s.Persoana)
            .Include(s => s.Consilier)
            .Where(s => s.HclId == hclId)
            .OrderBy(s => s.OrdineAfisare)
            .ToListAsync();

        return Ok(semnatari.Select(MapeazaSpreDto));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Adauga(int hclId, AdaugareSemnatarDto dto)
    {
        var hcl = await _context.Hcluri
            .Include(h => h.PunctOrdineZi).ThenInclude(p => p.Sedinta).ThenInclude(s => s.Institutie)
            .FirstOrDefaultAsync(h => h.Id == hclId);
        if (hcl is null) return NotFound("HCL inexistent.");

        if (hcl.Status == StatusActRedactional.Semnat)
            return Conflict("HCL semnat — lista de semnatari nu mai poate fi modificată.");

        // XOR Persoana/Consilier
        if (dto.PersoanaId.HasValue == dto.ConsilierId.HasValue)
            return BadRequest("Specifică exact unul dintre PersoanaId sau ConsilierId.");

        // FK corectă per rol (CK_SemnatarHcl_FkCorectaPerRol)
        var eroareRol = ValideazaRol(dto.Rol, dto.PersoanaId, dto.ConsilierId);
        if (eroareRol is not null) return BadRequest(eroareRol);

        // Existență subiect în tenant
        if (dto.PersoanaId.HasValue
            && !await _context.Persoane.AnyAsync(p => p.Id == dto.PersoanaId.Value))
            return NotFound("Persoana nu există.");
        if (dto.ConsilierId.HasValue
            && !await _context.Consilieri.AnyAsync(c => c.Id == dto.ConsilierId.Value))
            return NotFound("Consilierul nu există.");

        // Gărzi specifice art. 140 alin. 2
        if (dto.Rol == RolSemnatar.SemnatarAlternativArt140)
        {
            if (string.IsNullOrWhiteSpace(hcl.MotivLipsaSemnaturaPresedinte))
                return BadRequest("Setează mai întâi motivul lipsei semnăturii președintelui.");

            var estePrezent = await _context.Prezente.AnyAsync(p =>
                p.SedintaId == hcl.PunctOrdineZi.SedintaId
                && p.ConsilierId == dto.ConsilierId!.Value
                && (p.Status == StatusPrezenta.Prezent || p.Status == StatusPrezenta.OnlinePrezent));
            if (!estePrezent)
                return BadRequest("Semnatarul alternativ trebuie să fi fost prezent la ședință (Prezent sau OnlinePrezent).");
        }

        // Filtered unique pe rolurile unice (Președinte / Secretar UAT)
        if (dto.Rol == RolSemnatar.PresedinteSedinta || dto.Rol == RolSemnatar.SecretarUat)
        {
            var existaRol = await _context.SemnatariHcl
                .AnyAsync(s => s.HclId == hclId && s.RolSemnatar == dto.Rol);
            if (existaRol)
                return Conflict($"Există deja un semnatar cu rolul {dto.Rol.Eticheta()} pe acest HCL.");
        }

        // Conflict cross-rol pe același consilier
        if (dto.ConsilierId.HasValue)
        {
            var areDejaRol = await _context.SemnatariHcl
                .AnyAsync(s => s.HclId == hclId && s.ConsilierId == dto.ConsilierId.Value);
            if (areDejaRol)
                return Conflict("Consilierul are deja un rol de semnatar pe acest HCL.");
        }

        var dataSemnare = DateOnly.FromDateTime(
            hcl.DataAdoptare.LaFusOrar(hcl.PunctOrdineZi.Sedinta.Institutie.FusOrar));

        var semnatar = new SemnatarHcl
        {
            HclId = hclId,
            RolSemnatar = dto.Rol,
            PersoanaId = dto.PersoanaId,
            ConsilierId = dto.ConsilierId,
            DataSemnare = dataSemnare,
            OrdineAfisare = dto.OrdineAfisare
        };

        _context.SemnatariHcl.Add(semnatar);
        await _context.SaveChangesAsync();

        // Întoarce HclDetaliiDto: hub-ul își actualizează starea partajată (garda „Semnează"
        // depinde de lista de semnatari). Reload-ul cu include populează nume/persoană/consilier.
        return Ok(MapareHcl.SpreDetaliiDto(
            await _context.Hcluri.CuIncludeComplet().FirstAsync(h => h.Id == hclId)));
    }

    [HttpDelete("{semnatarId}")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Sterge(int hclId, int semnatarId)
    {
        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == hclId);
        if (hcl is null) return NotFound("HCL inexistent.");

        if (hcl.Status == StatusActRedactional.Semnat)
            return Conflict("HCL semnat — semnatarii fac parte din actul finalizat și nu mai pot fi eliminați.");

        var semnatar = await _context.SemnatariHcl
            .FirstOrDefaultAsync(s => s.Id == semnatarId && s.HclId == hclId);
        if (semnatar is null) return NotFound("Semnatarul nu există pe acest HCL.");

        var eraAlternativArt140 = semnatar.RolSemnatar == RolSemnatar.SemnatarAlternativArt140;

        _context.SemnatariHcl.Remove(semnatar);

        // Auto-clear motiv dacă tocmai am eliminat ultimul semnatar alternativ
        if (eraAlternativArt140)
        {
            var altiAlternativi = await _context.SemnatariHcl
                .AnyAsync(s => s.HclId == hclId
                            && s.RolSemnatar == RolSemnatar.SemnatarAlternativArt140
                            && s.Id != semnatarId);
            if (!altiAlternativi)
                hcl.MotivLipsaSemnaturaPresedinte = null;
        }

        await _context.SaveChangesAsync();

        // La fel ca POST: starea partajată a hub-ului reflectă lista actualizată de semnatari.
        return Ok(MapareHcl.SpreDetaliiDto(
            await _context.Hcluri.CuIncludeComplet().FirstAsync(h => h.Id == hclId)));
    }

    private static string? ValideazaRol(RolSemnatar rol, int? persoanaId, int? consilierId)
    {
        return rol switch
        {
            RolSemnatar.SecretarUat =>
                persoanaId.HasValue && !consilierId.HasValue
                    ? null
                    : "Secretar UAT trebuie să fie o Persoană (PersoanaId).",
            RolSemnatar.PresedinteSedinta or RolSemnatar.SemnatarAlternativArt140 =>
                consilierId.HasValue && !persoanaId.HasValue
                    ? null
                    : "Președintele și semnatarii alternativi trebuie să fie consilieri (ConsilierId).",
            _ => "Rol de semnatar invalid."
        };
    }

    private static SemnatarHclDto MapeazaSpreDto(SemnatarHcl s) => new(
        s.Id, s.RolSemnatar, s.PersoanaId, s.ConsilierId,
        s.Persoana?.NumeComplet ?? s.Consilier?.NumeComplet ?? "—",
        s.DataSemnare, s.OrdineAfisare);
}