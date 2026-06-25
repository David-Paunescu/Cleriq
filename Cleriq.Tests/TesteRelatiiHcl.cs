using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteRelatiiHcl
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteRelatiiHcl(CleriqFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task Adauga_RelatieInterna_ApareInAmbeleCapete()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl1 = await admin.GenereazaHclAdoptatAsync();
            var hcl2 = await admin.GenereazaHclAdoptatAsync();

            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl1.HclId}/Relatii", new
            {
                hclTintaId = hcl2.HclId,
                referintaActExternText = (string?)null,
                tipRelatie = TipRelatieHcl.Abroga
            });
            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

            var laSursa = await admin.GetFromJsonAsync<JsonElement>($"/api/Hcl/{hcl1.HclId}/Relatii");
            Assert.Equal(1, laSursa.GetProperty("relatiiSursa").GetArrayLength());
            Assert.Equal(0, laSursa.GetProperty("relatiiTinta").GetArrayLength());

            var laTinta = await admin.GetFromJsonAsync<JsonElement>($"/api/Hcl/{hcl2.HclId}/Relatii");
            Assert.Equal(0, laTinta.GetProperty("relatiiSursa").GetArrayLength());
            Assert.Equal(1, laTinta.GetProperty("relatiiTinta").GetArrayLength());
        }
    }

    [Fact]
    public async Task Adauga_RelatieActExtern_200()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Relatii", new
            {
                hclTintaId = (int?)null,
                referintaActExternText = "Legea nr. 52/2003",
                tipRelatie = TipRelatieHcl.PuneInAplicare
            });
            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

            var laSursa = await admin.GetFromJsonAsync<JsonElement>($"/api/Hcl/{hcl.HclId}/Relatii");
            var sursa = laSursa.GetProperty("relatiiSursa");
            Assert.Equal(1, sursa.GetArrayLength());
            Assert.Equal("Legea nr. 52/2003",
                sursa[0].GetProperty("referintaActExternText").GetString());
        }
    }

    [Fact]
    public async Task Adauga_FaraTintaSiFaraText_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Relatii", new
            {
                hclTintaId = (int?)null,
                referintaActExternText = (string?)null,
                tipRelatie = TipRelatieHcl.Modifica
            });
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Adauga_AutoReferinta_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Relatii", new
            {
                hclTintaId = hcl.HclId,
                referintaActExternText = (string?)null,
                tipRelatie = TipRelatieHcl.Modifica
            });
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Adauga_TextExternPesteLimita_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Relatii", new
            {
                hclTintaId = (int?)null,
                referintaActExternText = new string('x', 301),
                tipRelatie = TipRelatieHcl.PuneInAplicare
            });
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Adauga_DuplicatAcelasiTip_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl1 = await admin.GenereazaHclAdoptatAsync();
            var hcl2 = await admin.GenereazaHclAdoptatAsync();

            object corp = new { hclTintaId = hcl2.HclId, referintaActExternText = (string?)null, tipRelatie = TipRelatieHcl.Abroga };
            var prima = await admin.PostAsJsonAsync($"/api/Hcl/{hcl1.HclId}/Relatii", corp);
            Assert.Equal(HttpStatusCode.OK, prima.StatusCode);

            var aDoua = await admin.PostAsJsonAsync($"/api/Hcl/{hcl1.HclId}/Relatii", corp);
            Assert.Equal(HttpStatusCode.Conflict, aDoua.StatusCode);
        }
    }

    [Fact]
    public async Task Sterge_DinSursa_204_DinTinta_404()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl1 = await admin.GenereazaHclAdoptatAsync();
            var hcl2 = await admin.GenereazaHclAdoptatAsync();

            var adauga = await admin.PostAsJsonAsync($"/api/Hcl/{hcl1.HclId}/Relatii", new
            {
                hclTintaId = hcl2.HclId,
                referintaActExternText = (string?)null,
                tipRelatie = TipRelatieHcl.Completeaza
            });
            Assert.Equal(HttpStatusCode.OK, adauga.StatusCode);
            var relatieId = (await adauga.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            // din capătul țintă nu se poate șterge
            var dinTinta = await admin.DeleteAsync($"/api/Hcl/{hcl2.HclId}/Relatii/{relatieId}");
            Assert.Equal(HttpStatusCode.NotFound, dinTinta.StatusCode);

            // din capătul sursă da
            var dinSursa = await admin.DeleteAsync($"/api/Hcl/{hcl1.HclId}/Relatii/{relatieId}");
            Assert.Equal(HttpStatusCode.NoContent, dinSursa.StatusCode);
        }
    }

    private async Task<HttpClient> AdminNouAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }
}
