using Cleriq.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Data;

// Rulează DOAR în Development. Idempotență per instituție, cheia = slug
// (IgnoreQueryFilters: slug-urile soft-deleted sunt „arse" — nu recreăm).
public static class SeedDevelopment
{
    public static async Task RuleazaAsync(IServiceProvider sp, ILogger logger)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<Utilizator>>();

        await SeedSloboziaAsync(db, userManager, logger);
        await SeedFocsaniAsync(db, userManager, logger);
    }

    private static async Task SeedSloboziaAsync(
        AppDbContext db, UserManager<Utilizator> userManager, ILogger logger)
    {
        const string slug = "primaria-slobozia";
        if (await db.Institutii.IgnoreQueryFilters().AnyAsync(i => i.Slug == slug))
            return;

        var institutie = new Institutie
        {
            Denumire = "Primăria Slobozia",
            Slug = slug,
            Judet = "Ialomița",
            CodSiruta = "104454",
            Tip = TipInstitutie.Oras
        };
        db.Institutii.Add(institutie);
        await db.SaveChangesAsync();

        await CreeazaUtilizatorAsync(userManager,
            "admin.slobozia@cleriq.ro", "AdminSlobozia1!", "Admin Slobozia", "Admin", institutie.Id);
        await CreeazaUtilizatorAsync(userManager,
            "secretar.slobozia@cleriq.ro", "Secretar1234!", "Secretar Slobozia", "Secretar", institutie.Id);

        // InstitutieId explicit: la startup nu există tenant în context (pattern services system).
        var ion = new Consilier
        {
            NumeComplet = "Ion Popescu",
            Email = "ion.popescu@slobozia.ro",
            Activ = true,
            InstitutieId = institutie.Id
        };
        var vasile = new Consilier
        {
            NumeComplet = "Vasile Georgescu",
            Email = "vasile.georgescu@slobozia.ro",
            Activ = true,
            InstitutieId = institutie.Id
        };
        var testFiltru = new Consilier
        {
            NumeComplet = "Test Filtru",
            Activ = true,
            InstitutieId = institutie.Id
        };
        db.Consilieri.AddRange(ion, vasile, testFiltru);
        await db.SaveChangesAsync();

        await CreeazaUtilizatorAsync(userManager,
            "ion.popescu.cont@slobozia.ro", "Consilier1!", ion.NumeComplet, "Consilier",
            institutie.Id, consilierId: ion.Id);

        logger.LogInformation(
            "Seed Development: creată {Slug} (Admin, Secretar, 3 consilieri, 1 cont consilier).", slug);
    }

    private static async Task SeedFocsaniAsync(
        AppDbContext db, UserManager<Utilizator> userManager, ILogger logger)
    {
        const string slug = "primaria-focsani";
        if (await db.Institutii.IgnoreQueryFilters().AnyAsync(i => i.Slug == slug))
            return;

        var institutie = new Institutie
        {
            Denumire = "Primăria Focșani",
            Slug = slug,
            Judet = "Vrancea",
            CodSiruta = "176410",
            Tip = TipInstitutie.Municipiu
        };
        db.Institutii.Add(institutie);
        await db.SaveChangesAsync();

        await CreeazaUtilizatorAsync(userManager,
            "admin.focsani@cleriq.ro", "AdminFocsani1!", "Admin Focșani", "Admin", institutie.Id);

        logger.LogInformation("Seed Development: creată {Slug} (Admin).", slug);
    }

    private static async Task CreeazaUtilizatorAsync(
        UserManager<Utilizator> userManager, string email, string parola,
        string numeComplet, string rol, int institutieId, int? consilierId = null)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new Utilizator
        {
            UserName = email,
            Email = email,
            NumeComplet = numeComplet,
            InstitutieId = institutieId,
            ConsilierId = consilierId,
            EmailConfirmed = true
        };

        var rezultat = await userManager.CreateAsync(user, parola);
        if (!rezultat.Succeeded)
            throw new InvalidOperationException(
                $"Seed Development: eșec creare utilizator {email}: "
                + string.Join("; ", rezultat.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, rol);
    }
}