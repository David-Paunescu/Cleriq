using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteComisii
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteComisii(CleriqFixture fixture) => _factory = fixture.Factory;

    private async Task<HttpClient> AdminAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }

    // === CRUD comisie ===

    [Fact]
    public async Task Creeaza_Comisie_DtoPopulatCuMembriGoli()
    {
        using var admin = await AdminAsync();
        var raspuns = await admin.PostAsJsonAsync("/api/Comisii", new
        {
            denumire = "Comisie buget",
            descriere = "Buget, finanțe, administrare"
        });
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

        var dto = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Comisie buget", dto.GetProperty("denumire").GetString());
        Assert.Equal("Buget, finanțe, administrare", dto.GetProperty("descriere").GetString());
        Assert.Equal(0, dto.GetProperty("membri").GetArrayLength());
    }

    [Fact]
    public async Task Actualizeaza_Comisie_ReturneazaDateleNoi()
    {
        using var admin = await AdminAsync();
        var id = await admin.CreeazaComisieAsync("Vechi", "vechi");

        var raspuns = await admin.PutAsJsonAsync($"/api/Comisii/{id}", new
        {
            denumire = "Nou",
            descriere = (string?)null
        });
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

        var dto = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Nou", dto.GetProperty("denumire").GetString());
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("descriere").ValueKind);
    }

    [Fact]
    public async Task Sterge_Comisie_DetaliiE404()
    {
        using var admin = await AdminAsync();
        var id = await admin.CreeazaComisieAsync("De Sters");

        var stergere = await admin.DeleteAsync($"/api/Comisii/{id}");
        Assert.Equal(HttpStatusCode.NoContent, stergere.StatusCode);

        var verificare = await admin.GetAsync($"/api/Comisii/{id}");
        Assert.Equal(HttpStatusCode.NotFound, verificare.StatusCode);
    }

    // === AdaugaMembru ===

    [Fact]
    public async Task AdaugaMembru_DtoPopulat_DataInceputEstimataFalse()
    {
        using var admin = await AdminAsync();
        var comisieId = await admin.CreeazaComisieAsync("Comisie");
        var c = await admin.CreeazaConsilierAsync("Membru Test");

        var raspuns = await admin.PostAsJsonAsync($"/api/Comisii/{comisieId}/Membri", new
        {
            consilierId = c,
            rol = RolComisie.Presedinte,
            dataInceput = new DateOnly(2024, 1, 15)
        });
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

        var dto = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(c, dto.GetProperty("consilierId").GetInt32());
        Assert.Equal("Membru Test", dto.GetProperty("numeComplet").GetString());
        Assert.Equal((int)RolComisie.Presedinte, dto.GetProperty("rol").GetInt32());
        Assert.Equal("2024-01-15", dto.GetProperty("dataInceput").GetString());
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("dataSfarsit").ValueKind);
        Assert.False(dto.GetProperty("dataInceputEstimata").GetBoolean());
    }

    [Fact]
    public async Task AdaugaMembru_ConsilierInexistent_404()
    {
        using var admin = await AdminAsync();
        var comisieId = await admin.CreeazaComisieAsync("Comisie");

        var raspuns = await admin.PostAsJsonAsync($"/api/Comisii/{comisieId}/Membri", new
        {
            consilierId = 99999,
            rol = RolComisie.Membru,
            dataInceput = new DateOnly(2024, 1, 1)
        });
        Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
    }

    [Fact]
    public async Task AdaugaMembru_ComisieInexistenta_404()
    {
        using var admin = await AdminAsync();
        var c = await admin.CreeazaConsilierAsync("C");

        var raspuns = await admin.PostAsJsonAsync("/api/Comisii/99999/Membri", new
        {
            consilierId = c,
            rol = RolComisie.Membru,
            dataInceput = new DateOnly(2024, 1, 1)
        });
        Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
    }

    [Fact]
    public async Task AdaugaMembru_ConsilierDejaMembruActiv_409()
    {
        using var admin = await AdminAsync();
        var comisieId = await admin.CreeazaComisieAsync("Comisie");
        var c = await admin.CreeazaConsilierAsync("C");
        await admin.AdaugaMembruComisieAsync(comisieId, c, RolComisie.Membru, new DateOnly(2024, 1, 1));

        var raspuns = await admin.PostAsJsonAsync($"/api/Comisii/{comisieId}/Membri", new
        {
            consilierId = c,
            rol = RolComisie.Presedinte,
            dataInceput = new DateOnly(2025, 1, 1)
        });
        Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
    }

    // Refactor critic S46: re-adăugarea după Scoatere creează RÂND NOU, NU restore.
    // Bugul ar reapărea ca: includeIstoric=true returnează 1 rând (cel restaurat)
    // în loc de 2 (vechi închis + nou activ).
    [Fact]
    public async Task AdaugaMembru_DupaScoatere_CreeazaRandNou_NUFaceRestore()
    {
        using var admin = await AdminAsync();
        var comisieId = await admin.CreeazaComisieAsync("Comisie");
        var c = await admin.CreeazaConsilierAsync("Reintors");

        await admin.AdaugaMembruComisieAsync(
            comisieId, c, RolComisie.Membru, new DateOnly(2024, 1, 1));
        await admin.ScoateMembruComisieAsync(
            comisieId, c, new DateOnly(2024, 12, 31));
        await admin.AdaugaMembruComisieAsync(
            comisieId, c, RolComisie.Presedinte, new DateOnly(2025, 1, 1));

        // GET default: doar membria activă curentă
        var doarActivi = await admin.GetFromJsonAsync<JsonElement>($"/api/Comisii/{comisieId}");
        var activi = doarActivi.GetProperty("membri");
        Assert.Equal(1, activi.GetArrayLength());
        Assert.Equal("2025-01-01", activi[0].GetProperty("dataInceput").GetString());
        Assert.Equal(JsonValueKind.Null, activi[0].GetProperty("dataSfarsit").ValueKind);
        Assert.Equal((int)RolComisie.Presedinte, activi[0].GetProperty("rol").GetInt32());

        // GET includeIstoric=true: ambele rânduri (ORDER BY DataInceput → vechiul primul)
        var cuIstoric = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/Comisii/{comisieId}?includeIstoric=true");
        var toti = cuIstoric.GetProperty("membri");
        Assert.Equal(2, toti.GetArrayLength());

        Assert.Equal("2024-01-01", toti[0].GetProperty("dataInceput").GetString());
        Assert.Equal("2024-12-31", toti[0].GetProperty("dataSfarsit").GetString());
        Assert.Equal((int)RolComisie.Membru, toti[0].GetProperty("rol").GetInt32());

        Assert.Equal("2025-01-01", toti[1].GetProperty("dataInceput").GetString());
        Assert.Equal(JsonValueKind.Null, toti[1].GetProperty("dataSfarsit").ValueKind);
        Assert.Equal((int)RolComisie.Presedinte, toti[1].GetProperty("rol").GetInt32());

        Assert.Equal(c, toti[0].GetProperty("consilierId").GetInt32());
        Assert.Equal(c, toti[1].GetProperty("consilierId").GetInt32());
    }

    // === ScoateMembru ===

    [Fact]
    public async Task ScoateMembru_CuDataSfarsitExplicit_SeteazaCorect()
    {
        using var admin = await AdminAsync();
        var comisieId = await admin.CreeazaComisieAsync("Comisie");
        var c = await admin.CreeazaConsilierAsync("C");
        await admin.AdaugaMembruComisieAsync(
            comisieId, c, RolComisie.Membru, new DateOnly(2024, 1, 1));

        await admin.ScoateMembruComisieAsync(comisieId, c, new DateOnly(2024, 9, 30));

        var cuIstoric = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/Comisii/{comisieId}?includeIstoric=true");
        var membru = cuIstoric.GetProperty("membri")[0];
        Assert.Equal("2024-09-30", membru.GetProperty("dataSfarsit").GetString());
    }

    [Fact]
    public async Task ScoateMembru_FaraDataSfarsit_UtilizeazaAzi()
    {
        using var admin = await AdminAsync();
        var comisieId = await admin.CreeazaComisieAsync("Comisie");
        var c = await admin.CreeazaConsilierAsync("C");
        await admin.AdaugaMembruComisieAsync(
            comisieId, c, RolComisie.Membru, new DateOnly(2020, 1, 1));

        await admin.ScoateMembruComisieAsync(comisieId, c);

        var cuIstoric = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/Comisii/{comisieId}?includeIstoric=true");
        var ds = DateOnly.Parse(cuIstoric.GetProperty("membri")[0].GetProperty("dataSfarsit").GetString()!);
        var azi = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        Assert.True(ds == azi || ds == azi.AddDays(-1),
            $"Așteptat azi sau ieri UTC, primit {ds}");
    }

    [Fact]
    public async Task ScoateMembru_PeMembrieInexistentaSauDejaInchisa_404()
    {
        using var admin = await AdminAsync();
        var comisieId = await admin.CreeazaComisieAsync("Comisie");
        var cNeMembru = await admin.CreeazaConsilierAsync("Niciodată membru");
        var cInchis = await admin.CreeazaConsilierAsync("Membru închis");

        var faraMembrie = await admin.DeleteAsync($"/api/Comisii/{comisieId}/Membri/{cNeMembru}");
        Assert.Equal(HttpStatusCode.NotFound, faraMembrie.StatusCode);

        await admin.AdaugaMembruComisieAsync(
            comisieId, cInchis, RolComisie.Membru, new DateOnly(2024, 1, 1));
        await admin.ScoateMembruComisieAsync(comisieId, cInchis, new DateOnly(2024, 12, 31));

        var dejaInchis = await admin.DeleteAsync($"/api/Comisii/{comisieId}/Membri/{cInchis}");
        Assert.Equal(HttpStatusCode.NotFound, dejaInchis.StatusCode);
    }

    [Fact]
    public async Task ScoateMembru_DataSfarsitAnterioaraDataInceput_400()
    {
        using var admin = await AdminAsync();
        var comisieId = await admin.CreeazaComisieAsync("Comisie");
        var c = await admin.CreeazaConsilierAsync("C");
        await admin.AdaugaMembruComisieAsync(
            comisieId, c, RolComisie.Membru, new DateOnly(2024, 6, 1));

        var url = $"/api/Comisii/{comisieId}/Membri/{c}?dataSfarsit=2024-01-01";
        var raspuns = await admin.DeleteAsync(url);
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    // === Cross-tenant ===

    [Fact]
    public async Task Detalii_ComisieDinAltTenant_404()
    {
        var instA = await _factory.ProvisioneazaInstitutieAsync();
        var instB = await _factory.ProvisioneazaInstitutieAsync();
        using var adminA = await _factory.ClientAutentificatAsync(instA.EmailAdmin, instA.ParolaAdmin);
        using var adminB = await _factory.ClientAutentificatAsync(instB.EmailAdmin, instB.ParolaAdmin);

        var comisieB = await adminB.CreeazaComisieAsync("Comisie B");

        var raspuns = await adminA.GetAsync($"/api/Comisii/{comisieB}");
        Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
    }

    [Fact]
    public async Task AdaugaMembru_ConsilierDinAltTenant_404()
    {
        var instA = await _factory.ProvisioneazaInstitutieAsync();
        var instB = await _factory.ProvisioneazaInstitutieAsync();
        using var adminA = await _factory.ClientAutentificatAsync(instA.EmailAdmin, instA.ParolaAdmin);
        using var adminB = await _factory.ClientAutentificatAsync(instB.EmailAdmin, instB.ParolaAdmin);

        var comisieA = await adminA.CreeazaComisieAsync("Comisie A");
        var consilierB = await adminB.CreeazaConsilierAsync("C B");

        var raspuns = await adminA.PostAsJsonAsync($"/api/Comisii/{comisieA}/Membri", new
        {
            consilierId = consilierB,
            rol = RolComisie.Membru,
            dataInceput = new DateOnly(2024, 1, 1)
        });
        Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
    }
}