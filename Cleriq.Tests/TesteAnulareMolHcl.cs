using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteAnulareMolHcl
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteAnulareMolHcl(CleriqFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task AnulareMol_FaraMotiv_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await PublicatInMolAsync(admin);

            var anulare = await admin.AnuleazaMolAsync(hcl.HclId, "   ");
            Assert.Equal(HttpStatusCode.BadRequest, anulare.StatusCode);
        }
    }

    [Fact]
    public async Task AnulareMol_CuMotiv_FaraComunicare_200_LatchRamane()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await PublicatInMolAsync(admin);

            var anulare = await admin.AnuleazaMolAsync(hcl.HclId, "Corecție: dată de înregistrare greșită");
            Assert.Equal(HttpStatusCode.OK, anulare.StatusCode);

            var dto = await anulare.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Null, dto.GetProperty("dataPublicareMol").ValueKind);
            // latch-ul NU se resetează la anularea MOL
            Assert.True(dto.GetProperty("aIntratInCircuit").GetBoolean());
        }
    }

    [Fact]
    public async Task AnulareMol_DupaComunicareLaPrefect_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await PublicatInMolAsync(admin);
            await admin.AdaugaComunicarePrefectAsync(hcl.HclId);

            var anulare = await admin.AnuleazaMolAsync(hcl.HclId, "Vreau să anulez publicarea");
            Assert.Equal(HttpStatusCode.Conflict, anulare.StatusCode);
        }
    }

    // Portița închisă: anularea MOL nu redeschide varianta semnată (latch persistent).
    [Fact]
    public async Task AnulareMol_NuDezgheataVariantaSemnata_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await PublicatInMolAsync(admin);

            var prima = await admin.IncarcaHclSemnatAsync(
                hcl.HclId, Encoding.UTF8.GetBytes("%PDF-1.4 v1"), "semnat.pdf");
            Assert.Equal(HttpStatusCode.OK, prima.StatusCode);

            var anulare = await admin.AnuleazaMolAsync(hcl.HclId, "Corecție dată");
            Assert.Equal(HttpStatusCode.OK, anulare.StatusCode);

            var replace = await admin.IncarcaHclSemnatAsync(
                hcl.HclId, Encoding.UTF8.GetBytes("%PDF-1.4 v2"), "semnat-2.pdf");
            Assert.Equal(HttpStatusCode.Conflict, replace.StatusCode);

            var stergere = await admin.DeleteAsync($"/api/Hcl/{hcl.HclId}/Semnat");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    // Comunicarea la prefect (fără MOL) îngheață singură varianta semnată.
    [Fact]
    public async Task Comunicare_FaraMol_IngheataVariantaSemnata_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            await admin.SemneazaHclAsync(hcl.HclId);

            var prima = await admin.IncarcaHclSemnatAsync(
                hcl.HclId, Encoding.UTF8.GetBytes("%PDF-1.4 v1"), "semnat.pdf");
            Assert.Equal(HttpStatusCode.OK, prima.StatusCode);

            await admin.AdaugaComunicarePrefectAsync(hcl.HclId);

            var replace = await admin.IncarcaHclSemnatAsync(
                hcl.HclId, Encoding.UTF8.GetBytes("%PDF-1.4 v2"), "semnat-2.pdf");
            Assert.Equal(HttpStatusCode.Conflict, replace.StatusCode);

            var stergere = await admin.DeleteAsync($"/api/Hcl/{hcl.HclId}/Semnat");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    [Fact]
    public async Task AnulareMol_CaSecretar_403()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await PublicatInMolAsync(admin);
            var secretar = await _factory.ClientSecretarAsync(admin);
            using (secretar)
            {
                var anulare = await secretar.AnuleazaMolAsync(hcl.HclId, "motiv");
                Assert.Equal(HttpStatusCode.Forbidden, anulare.StatusCode);
            }
        }
    }

    private static async Task<HclAdoptat> PublicatInMolAsync(HttpClient admin)
    {
        var hcl = await admin.GenereazaHclAdoptatAsync();
        await admin.AtribuieNumarHclAsync(hcl.HclId);
        await admin.SemneazaHclAsync(hcl.HclId);
        await admin.PublicaMolAsync(hcl.HclId);
        return hcl;
    }

    private async Task<HttpClient> AdminNouAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }
}
