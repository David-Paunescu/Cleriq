using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TestePersoane
{
    private readonly CleriqWebApplicationFactory _factory;

    public TestePersoane(CleriqFixture fixture) => _factory = fixture.Factory;

    private async Task<HttpClient> AdminAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }

    [Fact]
    public async Task Creeaza_CuToateCampurile_PersistaCorect()
    {
        using var admin = await AdminAsync();
        var raspuns = await admin.PostAsJsonAsync("/api/Persoane", new
        {
            numeComplet = "Ion Vasile",
            email = "ion.vasile@test.ro",
            telefon = "0720111222"
        });
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

        var dto = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Ion Vasile", dto.GetProperty("numeComplet").GetString());
        Assert.Equal("ion.vasile@test.ro", dto.GetProperty("email").GetString());
        Assert.Equal("+40720111222", dto.GetProperty("telefon").GetString());
        Assert.False(dto.GetProperty("areMandate").GetBoolean());
    }

    [Fact]
    public async Task Creeaza_NumeCompletGol_Returneaza400()
    {
        using var admin = await AdminAsync();
        var raspuns = await admin.PostAsJsonAsync("/api/Persoane", new
        {
            numeComplet = "   ",
            email = (string?)null,
            telefon = (string?)null
        });
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    [Fact]
    public async Task NormalizareTelefon_TreiFormate_StocateLaFelInE164()
    {
        using var admin = await AdminAsync();

        var national = await admin.PostAsJsonAsync("/api/Persoane",
            new { numeComplet = "Nat", telefon = "0720111222" });
        var natDto = await national.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("+40720111222", natDto.GetProperty("telefon").GetString());

        var international = await admin.PostAsJsonAsync("/api/Persoane",
            new { numeComplet = "Intl", telefon = "+40720111223" });
        var intlDto = await international.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("+40720111223", intlDto.GetProperty("telefon").GetString());

        var doubleZero = await admin.PostAsJsonAsync("/api/Persoane",
            new { numeComplet = "ZZ", telefon = "0040720111224" });
        var dzDto = await doubleZero.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("+40720111224", dzDto.GetProperty("telefon").GetString());
    }

    [Fact]
    public async Task NormalizareTelefon_FormatInvalid_Returneaza400()
    {
        using var admin = await AdminAsync();
        var raspuns = await admin.PostAsJsonAsync("/api/Persoane", new
        {
            numeComplet = "X",
            telefon = "abc123"
        });
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    [Fact]
    public async Task NormalizareTelefon_NullSauGol_RamaneNull()
    {
        using var admin = await AdminAsync();

        var fNull = await admin.PostAsJsonAsync("/api/Persoane",
            new { numeComplet = "A", telefon = (string?)null });
        var dtoNull = await fNull.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, dtoNull.GetProperty("telefon").ValueKind);

        var fGol = await admin.PostAsJsonAsync("/api/Persoane",
            new { numeComplet = "B", telefon = "" });
        var dtoGol = await fGol.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, dtoGol.GetProperty("telefon").ValueKind);
    }

    [Fact]
    public async Task Detalii_PersoanaInexistenta_Returneaza404()
    {
        using var admin = await AdminAsync();
        var raspuns = await admin.GetAsync("/api/Persoane/99999");
        Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
    }

    [Fact]
    public async Task Actualizeaza_ModificaCampurileSiTelefonulENormalizat()
    {
        using var admin = await AdminAsync();
        var id = await admin.CreeazaPersoanaAsync("Nume Vechi", "vechi@test.ro");

        var raspuns = await admin.PutAsJsonAsync($"/api/Persoane/{id}", new
        {
            numeComplet = "Nume Nou",
            email = "nou@test.ro",
            telefon = "0720333444"
        });
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

        var dto = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Nume Nou", dto.GetProperty("numeComplet").GetString());
        Assert.Equal("nou@test.ro", dto.GetProperty("email").GetString());
        Assert.Equal("+40720333444", dto.GetProperty("telefon").GetString());
    }

    [Fact]
    public async Task Sterge_FaraMandate_204_ApoiDetaliiE404()
    {
        using var admin = await AdminAsync();
        var id = await admin.CreeazaPersoanaAsync("De Sters");

        var stergere = await admin.DeleteAsync($"/api/Persoane/{id}");
        Assert.Equal(HttpStatusCode.NoContent, stergere.StatusCode);

        var verificare = await admin.GetAsync($"/api/Persoane/{id}");
        Assert.Equal(HttpStatusCode.NotFound, verificare.StatusCode);
    }

    [Fact]
    public async Task Sterge_CuMandatActiv_Returneaza409()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("Primar Activ");
        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pId, null,
            new DateOnly(2024, 1, 1), null, "HCL 1/2024");

        var raspuns = await admin.DeleteAsync($"/api/Persoane/{pId}");
        Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
    }

    [Fact]
    public async Task Sterge_CuMandatInchisExplicit_Returneaza409()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("Primar Inchis");
        var mId = await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pId, null,
            new DateOnly(2020, 1, 1), null, "HCL 1/2020");
        await admin.InchideMandatFunctieAsync(mId, new DateOnly(2024, 12, 31));

        var raspuns = await admin.DeleteAsync($"/api/Persoane/{pId}");
        Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
    }

    [Fact]
    public async Task Sterge_CuMandatSoftDeleted_Returneaza409()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("Primar SD");
        var mId = await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pId, null,
            new DateOnly(2020, 1, 1), null, "HCL 1/2020");

        var stergeMandat = await admin.DeleteAsync($"/api/MandateFunctie/{mId}");
        Assert.Equal(HttpStatusCode.NoContent, stergeMandat.StatusCode);

        var raspuns = await admin.DeleteAsync($"/api/Persoane/{pId}");
        Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
    }

    [Fact]
    public async Task AreMandate_FlagulSeActualizeazaInListaSiDetalii()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("Persoana CuMandat");

        var fara = await admin.GetFromJsonAsync<JsonElement>($"/api/Persoane/{pId}");
        Assert.False(fara.GetProperty("areMandate").GetBoolean());

        await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pId, null, new DateOnly(2024, 1, 1));

        var cu = await admin.GetFromJsonAsync<JsonElement>($"/api/Persoane/{pId}");
        Assert.True(cu.GetProperty("areMandate").GetBoolean());

        var lista = await admin.GetFromJsonAsync<JsonElement>("/api/Persoane");
        var persoana = lista.EnumerateArray()
            .First(p => p.GetProperty("id").GetInt32() == pId);
        Assert.True(persoana.GetProperty("areMandate").GetBoolean());
    }

    // Bugul S47: IgnoreQueryFilters dintr-un subquery propaga la outer, dezactivând
    // filtrul global tenant → GET /api/Persoane vedea persoanele altui tenant.
    // Reproducerea: tenantul B are mandate (sub-query hit), tenantul A nu — dacă
    // bugul reapare prin refactor, GET A vede persoanele B.
    [Fact]
    public async Task Lista_NuExpunePersoaneDinAltTenant_RegresieS47()
    {
        var instA = await _factory.ProvisioneazaInstitutieAsync();
        var instB = await _factory.ProvisioneazaInstitutieAsync();
        using var adminA = await _factory.ClientAutentificatAsync(instA.EmailAdmin, instA.ParolaAdmin);
        using var adminB = await _factory.ClientAutentificatAsync(instB.EmailAdmin, instB.ParolaAdmin);

        var pB_cu = await adminB.CreeazaPersoanaAsync("B Cu Mandat");
        await adminB.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pB_cu, null, new DateOnly(2024, 1, 1));
        await adminB.CreeazaPersoanaAsync("B Fara Mandat");

        var listaA_gol = await adminA.GetFromJsonAsync<JsonElement>("/api/Persoane");
        Assert.Equal(0, listaA_gol.GetArrayLength());

        var pA = await adminA.CreeazaPersoanaAsync("A Unic");
        var listaA = await adminA.GetFromJsonAsync<JsonElement>("/api/Persoane");
        Assert.Equal(1, listaA.GetArrayLength());
        Assert.Equal(pA, listaA[0].GetProperty("id").GetInt32());

        var listaB = await adminB.GetFromJsonAsync<JsonElement>("/api/Persoane");
        var idsB = listaB.EnumerateArray()
            .Select(p => p.GetProperty("id").GetInt32())
            .ToList();
        Assert.Equal(2, idsB.Count);
        Assert.DoesNotContain(pA, idsB);
    }

    [Fact]
    public async Task Detalii_PersoanaDinAltTenant_Returneaza404()
    {
        var instA = await _factory.ProvisioneazaInstitutieAsync();
        var instB = await _factory.ProvisioneazaInstitutieAsync();
        using var adminA = await _factory.ClientAutentificatAsync(instA.EmailAdmin, instA.ParolaAdmin);
        using var adminB = await _factory.ClientAutentificatAsync(instB.EmailAdmin, instB.ParolaAdmin);

        var pB = await adminB.CreeazaPersoanaAsync("Tinta B");
        var raspuns = await adminA.GetAsync($"/api/Persoane/{pB}");
        Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
    }

    [Fact]
    public async Task Sterge_PersoanaDinAltTenant_Returneaza404SiNuSterge()
    {
        var instA = await _factory.ProvisioneazaInstitutieAsync();
        var instB = await _factory.ProvisioneazaInstitutieAsync();
        using var adminA = await _factory.ClientAutentificatAsync(instA.EmailAdmin, instA.ParolaAdmin);
        using var adminB = await _factory.ClientAutentificatAsync(instB.EmailAdmin, instB.ParolaAdmin);

        var pB = await adminB.CreeazaPersoanaAsync("Tinta Stergere B");

        var stergere = await adminA.DeleteAsync($"/api/Persoane/{pB}");
        Assert.Equal(HttpStatusCode.NotFound, stergere.StatusCode);

        var verificare = await adminB.GetAsync($"/api/Persoane/{pB}");
        Assert.Equal(HttpStatusCode.OK, verificare.StatusCode);
    }
}