using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteInvalidareHcl
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteInvalidareHcl(CleriqFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task Invalidare_FaraRelatii_200_CampuriSetate()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);

            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Invalidare", new
            {
                motiv = MotivInvalidare.AnulatInstanta,
                refInvalidare = "Sent. civ. 100/2026",
                confirmaCuRelatiiActive = false
            });
            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
            // POST întoarce HclDetaliiDto: câmpurile de invalidare + colecțiile sunt în răspuns
            var dtoPost = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotEqual(JsonValueKind.Null, dtoPost.GetProperty("dataInvalidare").ValueKind);
            Assert.Equal(2, dtoPost.GetProperty("semnatari").EnumerateArray().Count());

            var detalii = await admin.GetFromJsonAsync<JsonElement>($"/api/Hcl/{hcl.HclId}");
            Assert.NotEqual(JsonValueKind.Null, detalii.GetProperty("dataInvalidare").ValueKind);
            Assert.Equal((int)MotivInvalidare.AnulatInstanta, detalii.GetProperty("motivInvalidare").GetInt32());
            Assert.Equal("Sent. civ. 100/2026", detalii.GetProperty("refInvalidare").GetString());
        }
    }

    [Fact]
    public async Task Invalidare_DejaInvalidat_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);

            var prima = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Invalidare", new
            {
                motiv = MotivInvalidare.Retractat,
                refInvalidare = (string?)null,
                confirmaCuRelatiiActive = false
            });
            Assert.Equal(HttpStatusCode.OK, prima.StatusCode);

            var aDoua = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Invalidare", new
            {
                motiv = MotivInvalidare.Retractat,
                refInvalidare = (string?)null,
                confirmaCuRelatiiActive = false
            });
            Assert.Equal(HttpStatusCode.Conflict, aDoua.StatusCode);
        }
    }

    [Fact]
    public async Task Invalidare_CuRelatiiActive_FaraConfirm_409_ApoiCuConfirm_200()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl1 = await admin.GenereazaHclAdoptatAsync();
            var hcl2 = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl1.HclId);

            var relatie = await admin.PostAsJsonAsync($"/api/Hcl/{hcl1.HclId}/Relatii", new
            {
                hclTintaId = hcl2.HclId,
                referintaActExternText = (string?)null,
                tipRelatie = TipRelatieHcl.Abroga
            });
            Assert.Equal(HttpStatusCode.OK, relatie.StatusCode);

            var faraConfirm = await admin.PostAsJsonAsync($"/api/Hcl/{hcl1.HclId}/Invalidare", new
            {
                motiv = MotivInvalidare.AbrogatHclUlterior,
                refInvalidare = (string?)null,
                confirmaCuRelatiiActive = false
            });
            Assert.Equal(HttpStatusCode.Conflict, faraConfirm.StatusCode);
            var corp = await faraConfirm.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(corp.GetProperty("relatiiSursaActive").GetArrayLength() >= 1);

            var cuConfirm = await admin.PostAsJsonAsync($"/api/Hcl/{hcl1.HclId}/Invalidare", new
            {
                motiv = MotivInvalidare.AbrogatHclUlterior,
                refInvalidare = (string?)null,
                confirmaCuRelatiiActive = true
            });
            Assert.Equal(HttpStatusCode.OK, cuConfirm.StatusCode);
        }
    }

    [Fact]
    public async Task AnuleazaInvalidare_200_CampuriResetate()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);

            var invalidare = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Invalidare", new
            {
                motiv = MotivInvalidare.AnulatPrefect,
                refInvalidare = (string?)null,
                confirmaCuRelatiiActive = false
            });
            Assert.Equal(HttpStatusCode.OK, invalidare.StatusCode);

            var anulare = await admin.DeleteAsync($"/api/Hcl/{hcl.HclId}/Invalidare");
            Assert.Equal(HttpStatusCode.OK, anulare.StatusCode);
            // DELETE întoarce Detalii cu câmpurile de invalidare resetate
            var dtoAnulare = await anulare.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Null, dtoAnulare.GetProperty("dataInvalidare").ValueKind);
            Assert.Equal(2, dtoAnulare.GetProperty("semnatari").EnumerateArray().Count());

            var detalii = await admin.GetFromJsonAsync<JsonElement>($"/api/Hcl/{hcl.HclId}");
            Assert.Equal(JsonValueKind.Null, detalii.GetProperty("dataInvalidare").ValueKind);
            Assert.Equal(JsonValueKind.Null, detalii.GetProperty("motivInvalidare").ValueKind);
        }
    }

    [Fact]
    public async Task AnuleazaInvalidare_PeHclNeinvalidat_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var anulare = await admin.DeleteAsync($"/api/Hcl/{hcl.HclId}/Invalidare");
            Assert.Equal(HttpStatusCode.Conflict, anulare.StatusCode);
        }
    }

    private async Task<HttpClient> AdminNouAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }
}
