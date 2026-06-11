using Cleriq.Models;
using Microsoft.AspNetCore.Identity;

namespace Cleriq.Data;

public static class SeedComun
{
    public static async Task RuleazaAsync(IServiceProvider sp, IConfiguration config, ILogger logger)
    {
        var roleManager = sp.GetRequiredService<RoleManager<Rol>>();
        foreach (var rol in new[] { "SuperAdmin", "Admin", "Secretar", "Consilier" })
        {
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new Rol { Name = rol });
        }

        var email = config["SuperAdmin:Email"];
        var parola = config["SuperAdmin:Parola"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(parola))
        {
            logger.LogWarning(
                "SuperAdmin:Email/Parola lipsesc din configurare. SuperAdmin NU a fost creat.");
            return;
        }

        var userManager = sp.GetRequiredService<UserManager<Utilizator>>();
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var superAdmin = new Utilizator
        {
            UserName = email,
            Email = email,
            NumeComplet = "Super Admin",
            InstitutieId = 0,
            EmailConfirmed = true
        };

        var rezultat = await userManager.CreateAsync(superAdmin, parola);
        if (!rezultat.Succeeded)
            throw new InvalidOperationException(
                "Seed: eșec creare SuperAdmin: "
                + string.Join("; ", rezultat.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
    }
}