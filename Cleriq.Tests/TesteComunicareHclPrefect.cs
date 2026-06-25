using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteComunicareHclPrefect
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteComunicareHclPrefect(CleriqFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task Adauga_PeHclNenumerotat_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync(); // Draft
            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Comunicari", CorpComunicare());
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Adauga_PeHclNumerotat_NumereDeOrdineSecvential()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);

            var prima = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Comunicari", CorpComunicare());
            Assert.Equal(HttpStatusCode.OK, prima.StatusCode);
            var dto1 = await prima.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, dto1.GetProperty("numarOrdineInRegistru").GetInt32());

            var aDoua = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Comunicari", CorpComunicare());
            Assert.Equal(HttpStatusCode.OK, aDoua.StatusCode);
            var dto2 = await aDoua.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(2, dto2.GetProperty("numarOrdineInRegistru").GetInt32());
        }
    }

    [Fact]
    public async Task Actualizeaza_AplicaRaspuns_PastreazaImutabilele()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            var creare = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Comunicari", CorpComunicare());
            var creat = await creare.Content.ReadFromJsonAsync<JsonElement>();
            var comunicareId = creat.GetProperty("id").GetInt32();
            var canalInitial = creat.GetProperty("canalTransmitere").GetInt32();
            var ordineInitiala = creat.GetProperty("numarOrdineInRegistru").GetInt32();

            var actualizare = await admin.PutAsJsonAsync(
                $"/api/Hcl/{hcl.HclId}/Comunicari/{comunicareId}", new
                {
                    raspuns = RaspunsPrefect.Acceptat,
                    dataRaspuns = DateOnly.FromDateTime(DateTime.UtcNow),
                    obiectiiMotivate = (string?)null,
                    observatiiInterne = "verificat intern",
                    nrInregistrarePrefect = "PR-123",
                    dataConfirmarePrefect = (DateOnly?)null
                });
            Assert.Equal(HttpStatusCode.OK, actualizare.StatusCode);

            var dupa = await actualizare.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((int)RaspunsPrefect.Acceptat, dupa.GetProperty("raspunsPrefect").GetInt32());
            Assert.Equal("PR-123", dupa.GetProperty("nrInregistrarePrefect").GetString());
            Assert.Equal(canalInitial, dupa.GetProperty("canalTransmitere").GetInt32());
            Assert.Equal(ordineInitiala, dupa.GetProperty("numarOrdineInRegistru").GetInt32());
        }
    }

    [Fact]
    public async Task Sterge_CaSecretar_403_CaAdmin_204()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            var creare = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Comunicari", CorpComunicare());
            var comunicareId = (await creare.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            using var secretar = await _factory.ClientSecretarAsync(admin);
            var caSecretar = await secretar.DeleteAsync($"/api/Hcl/{hcl.HclId}/Comunicari/{comunicareId}");
            Assert.Equal(HttpStatusCode.Forbidden, caSecretar.StatusCode);

            var caAdmin = await admin.DeleteAsync($"/api/Hcl/{hcl.HclId}/Comunicari/{comunicareId}");
            Assert.Equal(HttpStatusCode.NoContent, caAdmin.StatusCode);
        }
    }

    [Fact]
    public async Task Registru_ListeazaComunicareaCronologic()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var numar = await admin.AtribuieNumarHclAsync(hcl.HclId);
            var creare = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Comunicari", CorpComunicare());
            Assert.Equal(HttpStatusCode.OK, creare.StatusCode);

            var an = DateTime.UtcNow.Year;
            var registru = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/RegistruComunicariPrefect?an={an}");
            var randul = registru.EnumerateArray()
                .First(r => r.GetProperty("hclId").GetInt32() == hcl.HclId);
            Assert.Equal($"{numar}/{an}", randul.GetProperty("numarHclFormatat").GetString());
        }
    }

    [Fact]
    public async Task StergeHcl_CuComunicariActive_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            var creare = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Comunicari", CorpComunicare());
            Assert.Equal(HttpStatusCode.OK, creare.StatusCode);

            // garda #1 din matricea DELETE: registrul prefectului e inviolabil
            var stergere = await admin.DeleteAsync($"/api/Hcl/{hcl.HclId}");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    private static object CorpComunicare() => new
    {
        dataTrimiteri = DateOnly.FromDateTime(DateTime.UtcNow),
        canalTransmitere = CanalTransmiterePrefect.EmailOficial,
        nrInregistrarePrefect = (string?)null,
        dataConfirmarePrefect = (DateOnly?)null,
        observatiiInterne = (string?)null
    };

    private async Task<HttpClient> AdminNouAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }
}
