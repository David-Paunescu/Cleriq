using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

// Dashboard „HCL urgent de comunicat" — alerte pe termenul de 10 zile lucrătoare (art. 197).
[Collection("Cleriq")]
public class TesteAlerteT3
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteAlerteT3(CleriqFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task UrgentDeComunicat_HclInPrag_Apare()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);

            var lista = await admin.GetFromJsonAsync<JsonElement>("/api/Hcl/UrgentDeComunicat?prag=30");
            var randul = lista.EnumerateArray()
                .FirstOrDefault(r => r.GetProperty("hclId").GetInt32() == hcl.HclId);
            Assert.NotEqual(JsonValueKind.Undefined, randul.ValueKind);
            Assert.True(randul.GetProperty("zileRamase").GetInt32() > 0);
            Assert.NotEqual(JsonValueKind.Null, randul.GetProperty("dataLimitaComunicare").ValueKind);
        }
    }

    [Fact]
    public async Task UrgentDeComunicat_HclPestePrag_NuApare()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);

            // ședința e la UtcNow+7 → termenul e la ~15 zile lucrătoare, peste pragul de 3
            var lista = await admin.GetFromJsonAsync<JsonElement>("/api/Hcl/UrgentDeComunicat?prag=3");
            Assert.DoesNotContain(lista.EnumerateArray(),
                r => r.GetProperty("hclId").GetInt32() == hcl.HclId);
        }
    }

    [Fact]
    public async Task UrgentDeComunicat_ExcludeInvalidat()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            await DbTest.SeteazaDataAdoptareHclAsync(hcl.HclId, DateTime.UtcNow.AddDays(-30));
            var inv = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Invalidare", new
            {
                motiv = MotivInvalidare.AnulatInstanta,
                refInvalidare = (string?)null,
                confirmaCuRelatiiActive = false
            });
            Assert.Equal(HttpStatusCode.OK, inv.StatusCode);

            var lista = await admin.GetFromJsonAsync<JsonElement>("/api/Hcl/UrgentDeComunicat?prag=30");
            Assert.DoesNotContain(lista.EnumerateArray(),
                r => r.GetProperty("hclId").GetInt32() == hcl.HclId);
        }
    }

    [Fact]
    public async Task UrgentDeComunicat_ExcludeCeleCuComunicareInregistrata()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            var com = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Comunicari", new
            {
                dataTrimiteri = DateOnly.FromDateTime(DateTime.UtcNow),
                canalTransmitere = CanalTransmiterePrefect.EmailOficial,
                nrInregistrarePrefect = (string?)null,
                dataConfirmarePrefect = (DateOnly?)null,
                observatiiInterne = (string?)null
            });
            Assert.Equal(HttpStatusCode.OK, com.StatusCode);

            var lista = await admin.GetFromJsonAsync<JsonElement>("/api/Hcl/UrgentDeComunicat?prag=30");
            Assert.DoesNotContain(lista.EnumerateArray(),
                r => r.GetProperty("hclId").GetInt32() == hcl.HclId);
        }
    }

    [Fact]
    public async Task UrgentDeComunicat_TermenDepasit_ZileRamaseNegativ()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            await DbTest.SeteazaDataAdoptareHclAsync(hcl.HclId, DateTime.UtcNow.AddDays(-30));

            var lista = await admin.GetFromJsonAsync<JsonElement>("/api/Hcl/UrgentDeComunicat?prag=3");
            var randul = lista.EnumerateArray()
                .First(r => r.GetProperty("hclId").GetInt32() == hcl.HclId);
            Assert.True(randul.GetProperty("zileRamase").GetInt32() < 0);
        }
    }

    private async Task<HttpClient> AdminNouAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }
}
