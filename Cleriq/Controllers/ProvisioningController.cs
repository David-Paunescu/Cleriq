using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Helpers;
using Cleriq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class ProvisioningController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<Utilizator> _userManager;

    public ProvisioningController(AppDbContext context, UserManager<Utilizator> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> CreeazaInstitutieCuAdmin(CreareInstitutieCuAdminDto dto)
    {
        // 1. Determină slug-ul: explicit furnizat sau auto-derivat din denumire
        string slug;
        if (!string.IsNullOrWhiteSpace(dto.Slug))
        {
            var eroareFormat = Slugify.Valideaza(dto.Slug);
            if (eroareFormat is not null)
                return BadRequest(eroareFormat);
            slug = dto.Slug;
        }
        else
        {
            slug = Slugify.Genereaza(dto.Denumire);
            if (string.IsNullOrWhiteSpace(slug))
                return BadRequest(
                    "Nu s-a putut genera un slug valid din denumire. Furnizează un slug explicit în DTO.");
        }

        // 2. Verifică unicitate — INCLUSIV pentru soft-deleted (slug-uri arse, decizia 3)
        var slugLuat = await _context.Institutii
            .IgnoreQueryFilters()
            .AnyAsync(i => i.Slug == slug);

        if (slugLuat)
        {
            // Calculează sugestii care chiar sunt disponibile (nu duplicate la rândul lor)
            var candidate = Slugify.GenereazaSugestii(slug, 5).ToList();
            var ocupate = await _context.Institutii
                .IgnoreQueryFilters()
                .Where(i => candidate.Contains(i.Slug))
                .Select(i => i.Slug)
                .ToListAsync();
            var disponibile = candidate.Except(ocupate).Take(3).ToArray();

            return Conflict(new EroareSlugDto(
                $"Slug-ul '{slug}' este deja folosit.",
                disponibile));
        }

        // 3. Tranzacție atomică: instituție + primul Admin
        using var tranzactie = await _context.Database.BeginTransactionAsync();

        try
        {
            var institutie = new Institutie
            {
                Denumire = dto.Denumire,
                Slug = slug,
                Judet = dto.Judet,
                CodSiruta = dto.CodSiruta,
                Tip = dto.Tip
            };
            _context.Institutii.Add(institutie);
            await _context.SaveChangesAsync();

            var admin = new Utilizator
            {
                UserName = dto.EmailAdmin,
                Email = dto.EmailAdmin,
                NumeComplet = dto.NumeCompletAdmin,
                InstitutieId = institutie.Id,
                EmailConfirmed = true
            };

            var rezultat = await _userManager.CreateAsync(admin, dto.ParolaAdmin);
            if (!rezultat.Succeeded)
            {
                await tranzactie.RollbackAsync();
                return BadRequest(rezultat.Errors);
            }

            await _userManager.AddToRoleAsync(admin, "Admin");
            await tranzactie.CommitAsync();

            return Ok(new RezultatProvisioningDto(
                institutie.Id, institutie.Denumire, institutie.Slug, admin.Id, admin.Email!));
        }
        catch
        {
            await tranzactie.RollbackAsync();
            throw;
        }
    }
}