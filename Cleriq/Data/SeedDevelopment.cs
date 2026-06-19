using Cleriq.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Data;

public static class SeedDevelopment
{
    private static readonly DateOnly DataInceputMandate = new(2024, 10, 27);

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

        // Mandate de consilier — precondiție logică pentru viceprimar (acoperă perioada)
        db.Mandate.AddRange(
            CreeazaMandatConsilier(ion.Id, institutie.Id),
            CreeazaMandatConsilier(vasile.Id, institutie.Id),
            CreeazaMandatConsilier(testFiltru.Id, institutie.Id));
        await db.SaveChangesAsync();

        await CreeazaUtilizatorAsync(userManager,
            "ion.popescu.cont@slobozia.ro", "Consilier1!", ion.NumeComplet, "Consilier",
            institutie.Id, consilierId: ion.Id);

        // Persoane pentru funcții ne-consilier (Primar, Secretar UAT)
        var primarSlobozia = new Persoana
        {
            NumeComplet = "Andrei Mihalache",
            Email = "primar@slobozia.ro",
            InstitutieId = institutie.Id
        };
        var secretarUatSlobozia = new Persoana
        {
            NumeComplet = "Maria Ionescu",
            Email = "secretar.uat@slobozia.ro",
            InstitutieId = institutie.Id
        };
        db.Persoane.AddRange(primarSlobozia, secretarUatSlobozia);
        await db.SaveChangesAsync();

        // Mandate de funcție active: Primar, Secretar UAT, Viceprimar (Ion)
        db.MandateFunctie.AddRange(
            new MandatFunctie
            {
                TipFunctie = TipFunctie.Primar,
                PersoanaId = primarSlobozia.Id,
                DataInceput = DataInceputMandate,
                NrActNumire = "HCL 1/2024",
                InstitutieId = institutie.Id
            },
            new MandatFunctie
            {
                TipFunctie = TipFunctie.SecretarUat,
                PersoanaId = secretarUatSlobozia.Id,
                DataInceput = DataInceputMandate,
                NrActNumire = "Ordin Prefect 123/2024",
                InstitutieId = institutie.Id
            },
            new MandatFunctie
            {
                TipFunctie = TipFunctie.Viceprimar,
                ConsilierId = ion.Id,
                DataInceput = DataInceputMandate,
                NrActNumire = "HCL 2/2024",
                InstitutieId = institutie.Id
            });
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Seed Development: creată {Slug} (Admin, Secretar, 3 consilieri + mandate, 1 cont consilier, 2 persoane, 3 mandate de funcție).",
            slug);
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

        // Primar diferit la Focșani — util pentru testul de izolare tenant pe FunctiiIstorice
        var primarFocsani = new Persoana
        {
            NumeComplet = "Costel Voinea",
            Email = "primar@focsani.ro",
            InstitutieId = institutie.Id
        };
        db.Persoane.Add(primarFocsani);
        await db.SaveChangesAsync();

        db.MandateFunctie.Add(new MandatFunctie
        {
            TipFunctie = TipFunctie.Primar,
            PersoanaId = primarFocsani.Id,
            DataInceput = DataInceputMandate,
            NrActNumire = "HCL 1/2024",
            InstitutieId = institutie.Id
        });
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Seed Development: creată {Slug} (Admin, 1 persoană, 1 mandat de funcție).", slug);
    }

    private static Mandat CreeazaMandatConsilier(int consilierId, int institutieId) => new()
    {
        ConsilierId = consilierId,
        DataInceput = DataInceputMandate,
        DataSfarsit = null,
        GrupPolitic = "Independent",
        InstitutieId = institutieId
    };

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