using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteNumerotareHcl
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteNumerotareHcl(CleriqFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task AtribuieNumar_PeDraft_Numerotat_SiInlocuiestePlaceholderul()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();

            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/AtribuieNumar",
                new { numar = 1, confirmaCuLacune = false });
            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

            var detalii = await admin.GetFromJsonAsync<JsonElement>($"/api/Hcl/{hcl.HclId}");
            Assert.Equal((int)StatusActRedactional.Numerotat, detalii.GetProperty("status").GetInt32());
            var numar = detalii.GetProperty("numar").GetInt32();
            var an = detalii.GetProperty("anNumerotare").GetInt32();
            Assert.Equal(1, numar);

            // placeholderul din titlu e înlocuit cu numărul real (fix S51)
            var continut = detalii.GetProperty("continut").GetString()!;
            Assert.Contains($"{numar}/{an}", continut);
            Assert.DoesNotContain("_[urmează să fie atribuit]_", continut);
        }
    }

    [Fact]
    public async Task AtribuieNumar_NumarZero_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/AtribuieNumar",
                new { numar = 0, confirmaCuLacune = false });
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task AtribuieNumar_PeHclDejaNumerotat_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId, 1);

            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/AtribuieNumar",
                new { numar = 2, confirmaCuLacune = false });
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task AtribuieNumar_LacuneNeconfirmate_409CuLacune_ApoiConfirmat_200()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();

            var faraConfirm = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/AtribuieNumar",
                new { numar = 5, confirmaCuLacune = false });
            Assert.Equal(HttpStatusCode.Conflict, faraConfirm.StatusCode);
            var corp = await faraConfirm.Content.ReadFromJsonAsync<JsonElement>();
            var lacune = corp.GetProperty("lacune").EnumerateArray().Select(x => x.GetInt32()).ToList();
            Assert.Equal(new[] { 1, 2, 3, 4 }, lacune);

            var cuConfirm = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/AtribuieNumar",
                new { numar = 5, confirmaCuLacune = true });
            Assert.Equal(HttpStatusCode.OK, cuConfirm.StatusCode);
            var dto = await cuConfirm.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(5, dto.GetProperty("numar").GetInt32());
        }
    }

    [Fact]
    public async Task AtribuieNumar_NumarActivLuatDeAltHcl_409CuSugestie()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl1 = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl1.HclId, 1);

            var hcl2 = await admin.GenereazaHclAdoptatAsync();
            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl2.HclId}/AtribuieNumar",
                new { numar = 1, confirmaCuLacune = false });
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
            var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(2, corp.GetProperty("sugestieAlternativa").GetInt32());
        }
    }

    [Fact]
    public async Task AtribuieNumar_NumarArs_NuPoateFiRefolosit_409CuSugestie()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl1 = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl1.HclId, 1);
            var stergere = await admin.DeleteAsync($"/api/Hcl/{hcl1.HclId}"); // arde nr. 1
            Assert.Equal(HttpStatusCode.NoContent, stergere.StatusCode);

            var hcl2 = await admin.GenereazaHclAdoptatAsync();
            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl2.HclId}/AtribuieNumar",
                new { numar = 1, confirmaCuLacune = false });
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
            var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(2, corp.GetProperty("sugestieAlternativa").GetInt32());
        }
    }

    [Fact]
    public async Task SugestieNumar_UrmatorulNumarLiber_SiAnulAdoptarii()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl1 = await admin.GenereazaHclAdoptatAsync();

            var s1 = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Hcl/{hcl1.HclId}/SugestieNumar");
            Assert.Equal(1, s1.GetProperty("numar").GetInt32());
            var anSugerat = s1.GetProperty("an").GetInt32();

            // anul sugerat coincide cu anul real de numerotare (din DataAdoptare)
            await admin.AtribuieNumarHclAsync(hcl1.HclId, 1);
            var detalii = await admin.GetFromJsonAsync<JsonElement>($"/api/Hcl/{hcl1.HclId}");
            Assert.Equal(anSugerat, detalii.GetProperty("anNumerotare").GetInt32());

            // după ce nr.1 e luat, al doilea HCL primește sugestia 2
            var hcl2 = await admin.GenereazaHclAdoptatAsync();
            var s2 = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Hcl/{hcl2.HclId}/SugestieNumar");
            Assert.Equal(2, s2.GetProperty("numar").GetInt32());
        }
    }

    private async Task<HttpClient> AdminNouAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }
}
