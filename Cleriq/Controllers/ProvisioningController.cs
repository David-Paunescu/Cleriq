using Cleriq.Data;
using Cleriq.DTOs;
using Cleriq.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
        using var tranzactie = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Creează instituția
            var institutie = new Institutie
            {
                Denumire = dto.Denumire,
                Judet = dto.Judet,
                CodSiruta = dto.CodSiruta,
                Tip = dto.Tip
            };
            _context.Institutii.Add(institutie);
            await _context.SaveChangesAsync();

            // 2. Creează primul Admin pentru instituția nou-creată
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
                institutie.Id, institutie.Denumire, admin.Id, admin.Email!));
        }
        catch
        {
            await tranzactie.RollbackAsync();
            throw;
        }
    }
}