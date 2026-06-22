using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/Hcl/{hclId}/Relatii")]
[Authorize]
public class RelatiiHclController : ControllerBase
{
    private const int SqlServerErrorUniqueConstraint = 2601;
    private const int SqlServerErrorDuplicateKey = 2627;

    private readonly AppDbContext _context;

    public RelatiiHclController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Lista(int hclId)
    {
        var hclExista = await _context.Hcluri.AnyAsync(h => h.Id == hclId);
        if (!hclExista) return NotFound("HCL inexistent.");

        var relatii = await _context.RelatiiHcl
            .Include(r => r.HclSursa)
            .Include(r => r.HclTinta)
            .Where(r => r.HclSursaId == hclId || r.HclTintaId == hclId)
            .ToListAsync();

        return Ok(new
        {
            relatiiSursa = relatii.Where(r => r.HclSursaId == hclId).Select(MapeazaSpreDto).ToList(),
            relatiiTinta = relatii.Where(r => r.HclTintaId == hclId).Select(MapeazaSpreDto).ToList()
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Adauga(int hclId, CreareRelatieDto dto)
    {
        var hclExista = await _context.Hcluri.AnyAsync(h => h.Id == hclId);
        if (!hclExista) return NotFound("HCL inexistent.");

        // XOR țintă: ori HclTintaId (intern), ori ReferintaActExternText (extern)
        var areTintaInterna = dto.HclTintaId.HasValue;
        var areTextExtern = !string.IsNullOrWhiteSpace(dto.ReferintaActExternText);
        if (areTintaInterna == areTextExtern)
            return BadRequest("Specifică exact unul dintre HclTintaId (HCL intern) sau ReferintaActExternText (act extern).");

        if (areTextExtern && dto.ReferintaActExternText!.Trim().Length > 300)
            return BadRequest("Referința actului extern nu poate depăși 300 de caractere.");

        if (areTintaInterna)
        {
            if (dto.HclTintaId!.Value == hclId)
                return BadRequest("Un HCL nu poate avea o relație cu el însuși.");

            var tintaExista = await _context.Hcluri.AnyAsync(h => h.Id == dto.HclTintaId.Value);
            if (!tintaExista) return NotFound("HCL-ul țintă nu există.");

            var existaDeja = await _context.RelatiiHcl.AnyAsync(r =>
                r.HclSursaId == hclId
                && r.HclTintaId == dto.HclTintaId.Value
                && r.TipRelatie == dto.TipRelatie);
            if (existaDeja)
                return Conflict("Există deja o relație de acest tip cu HCL-ul țintă.");
        }

        var relatie = new RelatieHcl
        {
            HclSursaId = hclId,
            HclTintaId = dto.HclTintaId,
            ReferintaActExternText = areTextExtern ? dto.ReferintaActExternText!.Trim() : null,
            TipRelatie = dto.TipRelatie
        };

        _context.RelatiiHcl.Add(relatie);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsteViolareUnique(ex))
        {
            return Conflict("Există deja o relație de acest tip cu HCL-ul țintă.");
        }

        await _context.Entry(relatie).Reference(r => r.HclSursa).LoadAsync();
        if (relatie.HclTintaId.HasValue)
            await _context.Entry(relatie).Reference(r => r.HclTinta).LoadAsync();

        return Ok(MapeazaSpreDto(relatie));
    }

    // Relațiile se șterg din contextul HCL-ului SURSĂ (cel care a declarat acțiunea).
    [HttpDelete("{relatieId}")]
    [Authorize(Roles = "Admin,Secretar")]
    public async Task<IActionResult> Sterge(int hclId, int relatieId)
    {
        var relatie = await _context.RelatiiHcl
            .FirstOrDefaultAsync(r => r.Id == relatieId && r.HclSursaId == hclId);
        if (relatie is null) return NotFound("Relația nu există ca relație-sursă pe acest HCL.");

        _context.RelatiiHcl.Remove(relatie);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static bool EsteViolareUnique(DbUpdateException ex)
        => ex.InnerException is SqlException sql
           && (sql.Number == SqlServerErrorUniqueConstraint
               || sql.Number == SqlServerErrorDuplicateKey);

    private static RelatieHclDto MapeazaSpreDto(RelatieHcl r) => new(
        r.Id, r.TipRelatie,
        r.HclSursaId, FormateazaNumar(r.HclSursa), r.HclSursa?.Titlu ?? "—",
        r.HclTintaId, FormateazaNumar(r.HclTinta), r.HclTinta?.Titlu,
        r.ReferintaActExternText);

    private static string? FormateazaNumar(Hcl? h)
        => h?.Numar != null && h.AnNumerotare != null ? $"{h.Numar}/{h.AnNumerotare}" : null;
}