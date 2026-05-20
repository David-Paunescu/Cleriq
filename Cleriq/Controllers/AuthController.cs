using Cleriq.DTOs;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static Cleriq.DTOs.AuthDto;

namespace Cleriq.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private static readonly string[] RoluriPermiseLaInregistrare = { "Admin", "Secretar", "Consilier" };

    private readonly UserManager<Utilizator> _userManager;
    private readonly IConfiguration _config;
    private readonly IFurnizorTenant _furnizorTenant;

    public AuthController(
        UserManager<Utilizator> userManager,
        IConfiguration config,
        IFurnizorTenant furnizorTenant)
    {
        _userManager = userManager;
        _config = config;
        _furnizorTenant = furnizorTenant;
    }

    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register(InregistrareDto dto)
    {
        if (!RoluriPermiseLaInregistrare.Contains(dto.Rol))
            return BadRequest($"Rol invalid. Permise: {string.Join(", ", RoluriPermiseLaInregistrare)}.");

        var institutieId = _furnizorTenant.InstitutieId;
        if (institutieId == 0)
            return BadRequest("Token-ul nu conține InstitutieId valid.");

        var user = new Utilizator
        {
            UserName = dto.Email,
            Email = dto.Email,
            NumeComplet = dto.NumeComplet,
            InstitutieId = institutieId
        };

        var rezultat = await _userManager.CreateAsync(user, dto.Parola);
        if (!rezultat.Succeeded)
            return BadRequest(rezultat.Errors);

        await _userManager.AddToRoleAsync(user, dto.Rol);
        return Ok(new { user.Id, user.Email, user.InstitutieId });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Parola))
            return Unauthorized("Email sau parolă greșite.");

        var roluri = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new("NumeComplet", user.NumeComplet),
            new("InstitutieId", user.InstitutieId.ToString())
        };
        claims.AddRange(roluri.Select(r => new Claim(ClaimTypes.Role, r)));

        var jwt = _config.GetSection("Jwt");
        var cheie = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var credentiale = new SigningCredentials(cheie, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpireMinutes"]!)),
            signingCredentials: credentiale);

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}