using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteFunctiiIstorice
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteFunctiiIstorice(CleriqFixture fixture) => _factory = fixture.Factory;

    private async Task<HttpClient> AdminAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }

    private static string F(DateOnly d) => d.ToString("yyyy-MM-dd");

    // === Primar / SecretarUat ===

    [Fact]
    public async Task PrimarLa_FaraNiciUnMandat_Null()
    {
        using var admin = await AdminAsync();
        var doc = await admin.GetFromJsonAsync<JsonElement>(
            "/api/FunctiiIstorice/Primar?data=2024-06-01");
        Assert.Equal(JsonValueKind.Null, doc.ValueKind);
    }

    [Fact]
    public async Task PrimarLa_BoundaryInclusivPeAmbeleCapete()
    {
        using var admin = await AdminAsync();
        var p = await admin.CreeazaPersoanaAsync("Andrei P");
        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, p, null,
            new DateOnly(2020, 1, 1), new DateOnly(2023, 12, 31));

        async Task AsertaPrimar(DateOnly data, string? numeAsteptat)
        {
            var doc = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/FunctiiIstorice/Primar?data={F(data)}");
            if (numeAsteptat is null)
                Assert.Equal(JsonValueKind.Null, doc.ValueKind);
            else
            {
                Assert.Equal(JsonValueKind.Object, doc.ValueKind);
                Assert.Equal(numeAsteptat, doc.GetProperty("numeComplet").GetString());
            }
        }

        await AsertaPrimar(new DateOnly(2019, 12, 31), null);
        await AsertaPrimar(new DateOnly(2020, 1, 1), "Andrei P");
        await AsertaPrimar(new DateOnly(2022, 6, 1), "Andrei P");
        await AsertaPrimar(new DateOnly(2023, 12, 31), "Andrei P");
        await AsertaPrimar(new DateOnly(2024, 1, 1), null);
    }

    [Fact]
    public async Task PrimarLa_PrimariConsecutivi_TranzitieClara()
    {
        using var admin = await AdminAsync();
        var pA = await admin.CreeazaPersoanaAsync("Primar A");
        var pB = await admin.CreeazaPersoanaAsync("Primar B");

        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pA, null,
            new DateOnly(2020, 1, 1), new DateOnly(2023, 12, 31));
        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pB, null, new DateOnly(2024, 1, 1));

        var la2022 = await admin.GetFromJsonAsync<JsonElement>(
            "/api/FunctiiIstorice/Primar?data=2022-06-01");
        Assert.Equal("Primar A", la2022.GetProperty("numeComplet").GetString());

        var la2024 = await admin.GetFromJsonAsync<JsonElement>(
            "/api/FunctiiIstorice/Primar?data=2024-06-01");
        Assert.Equal("Primar B", la2024.GetProperty("numeComplet").GetString());

        var la2030 = await admin.GetFromJsonAsync<JsonElement>(
            "/api/FunctiiIstorice/Primar?data=2030-01-01");
        Assert.Equal("Primar B", la2030.GetProperty("numeComplet").GetString());
    }

    [Fact]
    public async Task SecretarUatLa_TranzitieIntreDouaPersoane()
    {
        using var admin = await AdminAsync();
        var sA = await admin.CreeazaPersoanaAsync("Secretar A");
        var sB = await admin.CreeazaPersoanaAsync("Secretar B");

        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.SecretarUat, sA, null,
            new DateOnly(2018, 1, 1), new DateOnly(2022, 12, 31));
        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.SecretarUat, sB, null, new DateOnly(2023, 1, 1));

        var inainte = await admin.GetFromJsonAsync<JsonElement>(
            "/api/FunctiiIstorice/SecretarUat?data=2020-06-01");
        Assert.Equal("Secretar A", inainte.GetProperty("numeComplet").GetString());

        var dupa = await admin.GetFromJsonAsync<JsonElement>(
            "/api/FunctiiIstorice/SecretarUat?data=2024-06-01");
        Assert.Equal("Secretar B", dupa.GetProperty("numeComplet").GetString());
    }

    // === Viceprimari ===

    [Fact]
    public async Task Viceprimari_DoiSimultani_AmbiInLista()
    {
        using var admin = await AdminAsync();
        var c1 = await admin.CreeazaConsilierAsync("Vice 1");
        var c2 = await admin.CreeazaConsilierAsync("Vice 2");
        await admin.CreeazaMandatConsilierAsync(c1, new DateOnly(2020, 1, 1));
        await admin.CreeazaMandatConsilierAsync(c2, new DateOnly(2020, 1, 1));

        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Viceprimar, null, c1, new DateOnly(2020, 1, 1));
        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Viceprimar, null, c2, new DateOnly(2020, 1, 1));

        var lista = await admin.GetFromJsonAsync<JsonElement>(
            "/api/FunctiiIstorice/Viceprimari?data=2024-06-01");
        Assert.Equal(2, lista.GetArrayLength());

        var nume = lista.EnumerateArray()
            .Select(v => v.GetProperty("numeComplet").GetString()!)
            .OrderBy(n => n).ToList();
        Assert.Equal(new[] { "Vice 1", "Vice 2" }, nume);
    }

    // Cazul critic: filtrul preventiv din service exclude viceprimarii care nu mai
    // au mandat de consilier valid la data interogată. Dacă fix-ul dispare, testul
    // pică imediat (vor fi 1 rezultate în loc de 0 la 2024-06-01).
    [Fact]
    public async Task Viceprimari_FantomaDupaScurtareMandatConsilier_ExclusLa2024()
    {
        using var admin = await AdminAsync();
        var c = await admin.CreeazaConsilierAsync("C Fantomă");
        var mandatConsId = await admin.CreeazaMandatConsilierAsync(c, new DateOnly(2020, 1, 1));
        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Viceprimar, null, c, new DateOnly(2020, 1, 1));

        var initial = await admin.GetFromJsonAsync<JsonElement>(
            "/api/FunctiiIstorice/Viceprimari?data=2024-06-01");
        Assert.Equal(1, initial.GetArrayLength());

        await admin.ActualizeazaMandatConsilierAsync(
            mandatConsId, new DateOnly(2020, 1, 1), new DateOnly(2022, 12, 31));

        var dupa2024 = await admin.GetFromJsonAsync<JsonElement>(
            "/api/FunctiiIstorice/Viceprimari?data=2024-06-01");
        Assert.Equal(0, dupa2024.GetArrayLength());

        var inauntru2022 = await admin.GetFromJsonAsync<JsonElement>(
            "/api/FunctiiIstorice/Viceprimari?data=2022-06-01");
        Assert.Equal(1, inauntru2022.GetArrayLength());
    }

    // === MembriComisie + Presedinte ===

    [Fact]
    public async Task MembriComisie_DupaAdaugariSiScoatere_RezultatPerData()
    {
        using var admin = await AdminAsync();
        var c1 = await admin.CreeazaConsilierAsync("Mem 1");
        var c2 = await admin.CreeazaConsilierAsync("Mem 2");
        var comisieId = await admin.CreeazaComisieAsync("Comisie Test");

        await admin.AdaugaMembruComisieAsync(comisieId, c1, RolComisie.Membru, new DateOnly(2024, 1, 1));
        await admin.AdaugaMembruComisieAsync(comisieId, c2, RolComisie.Presedinte, new DateOnly(2024, 6, 1));

        var dor1 = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/FunctiiIstorice/Comisii/{comisieId}/Membri?data=2024-03-01");
        Assert.Equal(1, dor1.GetArrayLength());
        Assert.Equal("Mem 1", dor1[0].GetProperty("numeComplet").GetString());

        var ambii = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/FunctiiIstorice/Comisii/{comisieId}/Membri?data=2024-08-01");
        Assert.Equal(2, ambii.GetArrayLength());

        await admin.ScoateMembruComisieAsync(comisieId, c1, new DateOnly(2024, 9, 30));

        var dupaScoate = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/FunctiiIstorice/Comisii/{comisieId}/Membri?data=2024-12-01");
        Assert.Equal(1, dupaScoate.GetArrayLength());
        Assert.Equal("Mem 2", dupaScoate[0].GetProperty("numeComplet").GetString());
        Assert.Equal((int)RolComisie.Presedinte, dupaScoate[0].GetProperty("rol").GetInt32());

        // Boundary de închidere: la DataSfarsit, încă apare
        var boundaryClose = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/FunctiiIstorice/Comisii/{comisieId}/Membri?data=2024-09-30");
        Assert.Equal(2, boundaryClose.GetArrayLength());
    }

    [Fact]
    public async Task PresedinteComisieLa_PresedintiConsecutivi_TranzitieClara()
    {
        using var admin = await AdminAsync();
        var c1 = await admin.CreeazaConsilierAsync("Pres 1");
        var c2 = await admin.CreeazaConsilierAsync("Pres 2");
        var comisieId = await admin.CreeazaComisieAsync("Comisie");

        await admin.AdaugaMembruComisieAsync(comisieId, c1, RolComisie.Presedinte, new DateOnly(2020, 1, 1));
        await admin.ScoateMembruComisieAsync(comisieId, c1, new DateOnly(2023, 12, 31));
        await admin.AdaugaMembruComisieAsync(comisieId, c2, RolComisie.Presedinte, new DateOnly(2024, 1, 1));

        async Task AsertaPresedinte(DateOnly data, string? numeAsteptat)
        {
            var doc = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/FunctiiIstorice/Comisii/{comisieId}/Presedinte?data={F(data)}");
            if (numeAsteptat is null)
                Assert.Equal(JsonValueKind.Null, doc.ValueKind);
            else
                Assert.Equal(numeAsteptat, doc.GetProperty("numeComplet").GetString());
        }

        await AsertaPresedinte(new DateOnly(2022, 6, 1), "Pres 1");
        await AsertaPresedinte(new DateOnly(2023, 12, 31), "Pres 1");
        await AsertaPresedinte(new DateOnly(2024, 1, 1), "Pres 2");
        await AsertaPresedinte(new DateOnly(2025, 6, 1), "Pres 2");
        await AsertaPresedinte(new DateOnly(2019, 12, 31), null);
    }

    // === Consilieri ===

    [Fact]
    public async Task ConsilieriLa_NumaiCuMandatValid_TranzitieClara()
    {
        using var admin = await AdminAsync();
        var c1 = await admin.CreeazaConsilierAsync("Cons 1");
        var c2 = await admin.CreeazaConsilierAsync("Cons 2");
        var c3 = await admin.CreeazaConsilierAsync("Cons FaraMandat");

        await admin.CreeazaMandatConsilierAsync(
            c1, new DateOnly(2020, 1, 1), new DateOnly(2023, 12, 31));
        await admin.CreeazaMandatConsilierAsync(c2, new DateOnly(2024, 1, 1));

        async Task<List<string>> Lista(DateOnly data)
        {
            var doc = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/FunctiiIstorice/Consilieri?data={F(data)}");
            return doc.EnumerateArray()
                .Select(c => c.GetProperty("numeComplet").GetString()!)
                .OrderBy(n => n).ToList();
        }

        Assert.Equal(new[] { "Cons 1" }, await Lista(new DateOnly(2022, 6, 1)));
        Assert.Equal(new[] { "Cons 1" }, await Lista(new DateOnly(2023, 12, 31)));
        Assert.Equal(new[] { "Cons 2" }, await Lista(new DateOnly(2024, 1, 1)));
        Assert.Empty(await Lista(new DateOnly(2019, 12, 31)));

        // c3 nu apare niciodată (fără Mandat)
        var toate = (await Lista(new DateOnly(2024, 6, 1)))
            .Concat(await Lista(new DateOnly(2022, 6, 1)));
        Assert.DoesNotContain("Cons FaraMandat", toate);
    }

    // === Cross-tenant: regresie pentru IgnoreQueryFilters latent în serviciu ===

    [Fact]
    public async Task PrimarLa_CrossTenant_FiecareAdminVedeNumaiPropriul()
    {
        var instA = await _factory.ProvisioneazaInstitutieAsync();
        var instB = await _factory.ProvisioneazaInstitutieAsync();
        using var adminA = await _factory.ClientAutentificatAsync(instA.EmailAdmin, instA.ParolaAdmin);
        using var adminB = await _factory.ClientAutentificatAsync(instB.EmailAdmin, instB.ParolaAdmin);

        var pA = await adminA.CreeazaPersoanaAsync("Primar A");
        await adminA.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pA, null, new DateOnly(2020, 1, 1));

        var pB = await adminB.CreeazaPersoanaAsync("Primar B");
        await adminB.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pB, null, new DateOnly(2020, 1, 1));

        var rezA = await adminA.GetFromJsonAsync<JsonElement>(
            "/api/FunctiiIstorice/Primar?data=2024-06-01");
        Assert.Equal("Primar A", rezA.GetProperty("numeComplet").GetString());

        var rezB = await adminB.GetFromJsonAsync<JsonElement>(
            "/api/FunctiiIstorice/Primar?data=2024-06-01");
        Assert.Equal("Primar B", rezB.GetProperty("numeComplet").GetString());
    }

    // Pattern cu 3 queries în service (Mandate → Consilieri IgnoreQueryFilters → Mandate consilier).
    // Dacă filtrul tenant latent se sparge, una din liste va conține elemente străine.
    [Fact]
    public async Task Viceprimari_CrossTenant_FiecareAdminVedeNumaiProprii()
    {
        var instA = await _factory.ProvisioneazaInstitutieAsync();
        var instB = await _factory.ProvisioneazaInstitutieAsync();
        using var adminA = await _factory.ClientAutentificatAsync(instA.EmailAdmin, instA.ParolaAdmin);
        using var adminB = await _factory.ClientAutentificatAsync(instB.EmailAdmin, instB.ParolaAdmin);

        var cA = await adminA.CreeazaConsilierAsync("Vice A");
        await adminA.CreeazaMandatConsilierAsync(cA, new DateOnly(2020, 1, 1));
        await adminA.CreeazaMandatFunctieAsync(
            TipFunctie.Viceprimar, null, cA, new DateOnly(2020, 1, 1));

        var cB = await adminB.CreeazaConsilierAsync("Vice B");
        await adminB.CreeazaMandatConsilierAsync(cB, new DateOnly(2020, 1, 1));
        await adminB.CreeazaMandatFunctieAsync(
            TipFunctie.Viceprimar, null, cB, new DateOnly(2020, 1, 1));

        var lA = await adminA.GetFromJsonAsync<JsonElement>(
            "/api/FunctiiIstorice/Viceprimari?data=2024-06-01");
        Assert.Equal(1, lA.GetArrayLength());
        Assert.Equal("Vice A", lA[0].GetProperty("numeComplet").GetString());

        var lB = await adminB.GetFromJsonAsync<JsonElement>(
            "/api/FunctiiIstorice/Viceprimari?data=2024-06-01");
        Assert.Equal(1, lB.GetArrayLength());
        Assert.Equal("Vice B", lB[0].GetProperty("numeComplet").GetString());
    }
}