using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteMandateFunctie
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteMandateFunctie(CleriqFixture fixture) => _factory = fixture.Factory;

    private async Task<HttpClient> AdminAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }

    private static readonly DateOnly DataStart = new(2024, 10, 27);

    [Fact]
    public async Task Creeaza_Primar_DtoPopulatCuNumePersoana()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("Ana Primar");

        var raspuns = await admin.PostAsJsonAsync("/api/MandateFunctie", new
        {
            tipFunctie = TipFunctie.Primar,
            persoanaId = pId,
            consilierId = (int?)null,
            dataInceput = DataStart,
            dataSfarsit = (DateOnly?)null,
            nrActNumire = "  HCL 1/2024  "
        });
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

        var dto = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)TipFunctie.Primar, dto.GetProperty("tipFunctie").GetInt32());
        Assert.Equal(pId, dto.GetProperty("persoanaId").GetInt32());
        Assert.Equal("Ana Primar", dto.GetProperty("numeCompletPersoana").GetString());
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("consilierId").ValueKind);
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("numeCompletConsilier").ValueKind);
        Assert.Equal("HCL 1/2024", dto.GetProperty("nrActNumire").GetString());
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("dataSfarsit").ValueKind);
    }

    [Fact]
    public async Task Creeaza_Viceprimar_DtoPopulatCuNumeConsilier()
    {
        using var admin = await AdminAsync();
        var cId = await admin.CreeazaConsilierAsync("Bogdan Consilier");
        await admin.CreeazaMandatConsilierAsync(cId, DataStart);

        var mandatId = await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Viceprimar, null, cId, DataStart);

        var dto = await admin.GetFromJsonAsync<JsonElement>($"/api/MandateFunctie/{mandatId}");
        Assert.Equal((int)TipFunctie.Viceprimar, dto.GetProperty("tipFunctie").GetInt32());
        Assert.Equal(cId, dto.GetProperty("consilierId").GetInt32());
        Assert.Equal("Bogdan Consilier", dto.GetProperty("numeCompletConsilier").GetString());
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("persoanaId").ValueKind);
    }

    [Fact]
    public async Task Creeaza_SecretarUat_DtoPopulat()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("Maria Secretar");

        var mandatId = await admin.CreeazaMandatFunctieAsync(
            TipFunctie.SecretarUat, pId, null, DataStart, null, "Ordin Prefect 1/2024");

        var dto = await admin.GetFromJsonAsync<JsonElement>($"/api/MandateFunctie/{mandatId}");
        Assert.Equal((int)TipFunctie.SecretarUat, dto.GetProperty("tipFunctie").GetInt32());
        Assert.Equal(pId, dto.GetProperty("persoanaId").GetInt32());
    }

    [Fact]
    public async Task Creeaza_Primar_FaraPersoanaId_400()
    {
        using var admin = await AdminAsync();
        var raspuns = await admin.PostAsJsonAsync("/api/MandateFunctie", new
        {
            tipFunctie = TipFunctie.Primar,
            persoanaId = (int?)null,
            consilierId = (int?)null,
            dataInceput = DataStart
        });
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    [Fact]
    public async Task Creeaza_Primar_CuConsilierIdInLocDePersoanaId_400()
    {
        using var admin = await AdminAsync();
        var cId = await admin.CreeazaConsilierAsync("Greșit");
        var raspuns = await admin.PostAsJsonAsync("/api/MandateFunctie", new
        {
            tipFunctie = TipFunctie.Primar,
            persoanaId = (int?)null,
            consilierId = cId,
            dataInceput = DataStart
        });
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    [Fact]
    public async Task Creeaza_Primar_CuAmbeleSetate_400()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("P");
        var cId = await admin.CreeazaConsilierAsync("C");
        var raspuns = await admin.PostAsJsonAsync("/api/MandateFunctie", new
        {
            tipFunctie = TipFunctie.Primar,
            persoanaId = pId,
            consilierId = cId,
            dataInceput = DataStart
        });
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    [Fact]
    public async Task Creeaza_Viceprimar_FaraConsilierId_400()
    {
        using var admin = await AdminAsync();
        var raspuns = await admin.PostAsJsonAsync("/api/MandateFunctie", new
        {
            tipFunctie = TipFunctie.Viceprimar,
            persoanaId = (int?)null,
            consilierId = (int?)null,
            dataInceput = DataStart
        });
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    [Fact]
    public async Task Creeaza_Viceprimar_CuPersoanaIdInLocDeConsilierId_400()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("Greșit Vice");
        var raspuns = await admin.PostAsJsonAsync("/api/MandateFunctie", new
        {
            tipFunctie = TipFunctie.Viceprimar,
            persoanaId = pId,
            consilierId = (int?)null,
            dataInceput = DataStart
        });
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    [Fact]
    public async Task Creeaza_DataSfarsitAnterioaraDataInceput_400()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("P");
        var raspuns = await admin.PostAsJsonAsync("/api/MandateFunctie", new
        {
            tipFunctie = TipFunctie.Primar,
            persoanaId = pId,
            consilierId = (int?)null,
            dataInceput = new DateOnly(2024, 12, 31),
            dataSfarsit = new DateOnly(2024, 1, 1)
        });
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    [Fact]
    public async Task Creeaza_PersoanaInexistenta_404()
    {
        using var admin = await AdminAsync();
        var raspuns = await admin.PostAsJsonAsync("/api/MandateFunctie", new
        {
            tipFunctie = TipFunctie.Primar,
            persoanaId = 99999,
            consilierId = (int?)null,
            dataInceput = DataStart
        });
        Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
    }

    [Fact]
    public async Task Creeaza_ConsilierInexistent_404()
    {
        using var admin = await AdminAsync();
        var raspuns = await admin.PostAsJsonAsync("/api/MandateFunctie", new
        {
            tipFunctie = TipFunctie.Viceprimar,
            persoanaId = (int?)null,
            consilierId = 99999,
            dataInceput = DataStart
        });
        Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
    }

    // Apărare in depth: PersoanaId din alt tenant e invizibilă datorită filtrului
    // global → AnyAsync returnează false → 404 "Persoana nu există".
    [Fact]
    public async Task Creeaza_PersoanaIdDinAltTenant_404()
    {
        var instA = await _factory.ProvisioneazaInstitutieAsync();
        var instB = await _factory.ProvisioneazaInstitutieAsync();
        using var adminA = await _factory.ClientAutentificatAsync(instA.EmailAdmin, instA.ParolaAdmin);
        using var adminB = await _factory.ClientAutentificatAsync(instB.EmailAdmin, instB.ParolaAdmin);

        var pB = await adminB.CreeazaPersoanaAsync("Persoana B");

        var raspuns = await adminA.PostAsJsonAsync("/api/MandateFunctie", new
        {
            tipFunctie = TipFunctie.Primar,
            persoanaId = pB,
            consilierId = (int?)null,
            dataInceput = DataStart
        });
        Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
    }

    [Fact]
    public async Task Lista_NuExpuneMandateleAltuiTenant_RegresieS47()
    {
        var instA = await _factory.ProvisioneazaInstitutieAsync();
        var instB = await _factory.ProvisioneazaInstitutieAsync();
        using var adminA = await _factory.ClientAutentificatAsync(instA.EmailAdmin, instA.ParolaAdmin);
        using var adminB = await _factory.ClientAutentificatAsync(instB.EmailAdmin, instB.ParolaAdmin);

        var pB = await adminB.CreeazaPersoanaAsync("Primar B");
        await adminB.CreeazaMandatFunctieAsync(TipFunctie.Primar, pB, null, DataStart);

        var listaAgolă = await adminA.GetFromJsonAsync<JsonElement>("/api/MandateFunctie");
        Assert.Equal(0, listaAgolă.GetArrayLength());

        var pA = await adminA.CreeazaPersoanaAsync("Primar A");
        await adminA.CreeazaMandatFunctieAsync(TipFunctie.Primar, pA, null, DataStart);

        var listaA = await adminA.GetFromJsonAsync<JsonElement>("/api/MandateFunctie");
        Assert.Equal(1, listaA.GetArrayLength());
        Assert.Equal(pA, listaA[0].GetProperty("persoanaId").GetInt32());
    }

    [Fact]
    public async Task Lista_FiltruTipFunctie_ReturneazaDoarTipul()
    {
        using var admin = await AdminAsync();
        var pPrimar = await admin.CreeazaPersoanaAsync("P Primar");
        var pSecretar = await admin.CreeazaPersoanaAsync("P Secretar");
        var cVice = await admin.CreeazaConsilierAsync("C Vice");
        await admin.CreeazaMandatConsilierAsync(cVice, DataStart);

        await admin.CreeazaMandatFunctieAsync(TipFunctie.Primar, pPrimar, null, DataStart);
        await admin.CreeazaMandatFunctieAsync(TipFunctie.SecretarUat, pSecretar, null, DataStart);
        await admin.CreeazaMandatFunctieAsync(TipFunctie.Viceprimar, null, cVice, DataStart);

        var primari = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/MandateFunctie?tipFunctie={(int)TipFunctie.Primar}");
        Assert.Equal(1, primari.GetArrayLength());
        Assert.Equal((int)TipFunctie.Primar, primari[0].GetProperty("tipFunctie").GetInt32());

        var viceprimari = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/MandateFunctie?tipFunctie={(int)TipFunctie.Viceprimar}");
        Assert.Equal(1, viceprimari.GetArrayLength());
        Assert.Equal((int)TipFunctie.Viceprimar, viceprimari[0].GetProperty("tipFunctie").GetInt32());

        var toate = await admin.GetFromJsonAsync<JsonElement>("/api/MandateFunctie");
        Assert.Equal(3, toate.GetArrayLength());
    }

    [Fact]
    public async Task Detalii_DinAltTenant_404()
    {
        var instA = await _factory.ProvisioneazaInstitutieAsync();
        var instB = await _factory.ProvisioneazaInstitutieAsync();
        using var adminA = await _factory.ClientAutentificatAsync(instA.EmailAdmin, instA.ParolaAdmin);
        using var adminB = await _factory.ClientAutentificatAsync(instB.EmailAdmin, instB.ParolaAdmin);

        var pB = await adminB.CreeazaPersoanaAsync("Tinta B");
        var mB = await adminB.CreeazaMandatFunctieAsync(TipFunctie.Primar, pB, null, DataStart);

        var raspuns = await adminA.GetAsync($"/api/MandateFunctie/{mB}");
        Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
    }

    [Fact]
    public async Task Actualizeaza_DateleSeSchimba()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("P");
        var mId = await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pId, null, DataStart, null, "HCL vechi");

        var raspuns = await admin.PutAsJsonAsync($"/api/MandateFunctie/{mId}", new
        {
            dataInceput = new DateOnly(2024, 11, 1),
            dataSfarsit = new DateOnly(2028, 10, 31),
            nrActNumire = "  HCL nou  "
        });
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

        var dto = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2024-11-01", dto.GetProperty("dataInceput").GetString());
        Assert.Equal("2028-10-31", dto.GetProperty("dataSfarsit").GetString());
        Assert.Equal("HCL nou", dto.GetProperty("nrActNumire").GetString());
    }

    [Fact]
    public async Task Actualizeaza_DataSfarsitAnterioaraDataInceput_400()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("P");
        var mId = await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pId, null, DataStart);

        var raspuns = await admin.PutAsJsonAsync($"/api/MandateFunctie/{mId}", new
        {
            dataInceput = new DateOnly(2024, 12, 31),
            dataSfarsit = new DateOnly(2024, 1, 1),
            nrActNumire = (string?)null
        });
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    [Fact]
    public async Task Actualizeaza_DinAltTenant_404()
    {
        var instA = await _factory.ProvisioneazaInstitutieAsync();
        var instB = await _factory.ProvisioneazaInstitutieAsync();
        using var adminA = await _factory.ClientAutentificatAsync(instA.EmailAdmin, instA.ParolaAdmin);
        using var adminB = await _factory.ClientAutentificatAsync(instB.EmailAdmin, instB.ParolaAdmin);

        var pB = await adminB.CreeazaPersoanaAsync("P B");
        var mB = await adminB.CreeazaMandatFunctieAsync(TipFunctie.Primar, pB, null, DataStart);

        var raspuns = await adminA.PutAsJsonAsync($"/api/MandateFunctie/{mB}", new
        {
            dataInceput = DataStart,
            dataSfarsit = (DateOnly?)null,
            nrActNumire = "X"
        });
        Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
    }

    [Fact]
    public async Task Inchide_PeMandatDeschis_SeteazaDataSfarsit()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("P");
        var mId = await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pId, null, DataStart);

        var ds = new DateOnly(2024, 12, 31);
        var raspuns = await admin.PostAsJsonAsync(
            $"/api/MandateFunctie/{mId}/Inchide", new { dataSfarsit = ds });
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

        var dto = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2024-12-31", dto.GetProperty("dataSfarsit").GetString());
    }

    [Fact]
    public async Task Inchide_PeMandatDejaInchis_409()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("P");
        var mId = await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pId, null, DataStart);
        await admin.InchideMandatFunctieAsync(mId, new DateOnly(2024, 12, 31));

        var raspuns = await admin.PostAsJsonAsync(
            $"/api/MandateFunctie/{mId}/Inchide",
            new { dataSfarsit = new DateOnly(2025, 6, 30) });
        Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
    }

    [Fact]
    public async Task Inchide_DataSfarsitAnterioaraDataInceput_400()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("P");
        var mId = await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pId, null, new DateOnly(2024, 6, 1));

        var raspuns = await admin.PostAsJsonAsync(
            $"/api/MandateFunctie/{mId}/Inchide",
            new { dataSfarsit = new DateOnly(2024, 1, 1) });
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    [Fact]
    public async Task Sterge_SoftDelete_ApoiDetaliiE404()
    {
        using var admin = await AdminAsync();
        var pId = await admin.CreeazaPersoanaAsync("P");
        var mId = await admin.CreeazaMandatFunctieAsync(
            TipFunctie.Primar, pId, null, DataStart);

        var stergere = await admin.DeleteAsync($"/api/MandateFunctie/{mId}");
        Assert.Equal(HttpStatusCode.NoContent, stergere.StatusCode);

        var verificare = await admin.GetAsync($"/api/MandateFunctie/{mId}");
        Assert.Equal(HttpStatusCode.NotFound, verificare.StatusCode);
    }

    [Fact]
    public async Task Sterge_DinAltTenant_404SiNuSterge()
    {
        var instA = await _factory.ProvisioneazaInstitutieAsync();
        var instB = await _factory.ProvisioneazaInstitutieAsync();
        using var adminA = await _factory.ClientAutentificatAsync(instA.EmailAdmin, instA.ParolaAdmin);
        using var adminB = await _factory.ClientAutentificatAsync(instB.EmailAdmin, instB.ParolaAdmin);

        var pB = await adminB.CreeazaPersoanaAsync("P B");
        var mB = await adminB.CreeazaMandatFunctieAsync(TipFunctie.Primar, pB, null, DataStart);

        var stergere = await adminA.DeleteAsync($"/api/MandateFunctie/{mB}");
        Assert.Equal(HttpStatusCode.NotFound, stergere.StatusCode);

        var verificare = await adminB.GetAsync($"/api/MandateFunctie/{mB}");
        Assert.Equal(HttpStatusCode.OK, verificare.StatusCode);
    }
}