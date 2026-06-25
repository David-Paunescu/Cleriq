using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteAnexeHcl
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteAnexeHcl(CleriqFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task Incarca_AnexaHcl_ForteazaTipDocumentAltele()
    {
        var (_, admin) = await InstitutieAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var raspuns = await admin.IncarcaAnexaHclAsync(
                hcl.HclId, Bytes("anexa"), "anexa.pdf", "Anexa 1", TipDocumentHcl.Anexa, 1);
            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

            var dto = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((int)TipDocument.Altele, dto.GetProperty("tipDocument").GetInt32());
            Assert.Equal((int)TipDocumentHcl.Anexa, dto.GetProperty("tipDocumentHcl").GetInt32());
            Assert.Equal(1, dto.GetProperty("numarOrdinAnexa").GetInt32());
        }
    }

    [Fact]
    public async Task Incarca_AnexaFaraNumarOrdine_400()
    {
        var (_, admin) = await InstitutieAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var raspuns = await admin.IncarcaAnexaHclAsync(
                hcl.HclId, Bytes("anexa"), "anexa.pdf", "Anexa", TipDocumentHcl.Anexa, null);
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Incarca_AnexaDuplicat_409()
    {
        var (_, admin) = await InstitutieAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var prima = await admin.IncarcaAnexaHclAsync(
                hcl.HclId, Bytes("a"), "a.pdf", "Anexa 1", TipDocumentHcl.Anexa, 1);
            Assert.Equal(HttpStatusCode.OK, prima.StatusCode);

            var aDoua = await admin.IncarcaAnexaHclAsync(
                hcl.HclId, Bytes("b"), "b.pdf", "Anexa 1 bis", TipDocumentHcl.Anexa, 1);
            Assert.Equal(HttpStatusCode.Conflict, aDoua.StatusCode);
        }
    }

    [Fact]
    public async Task Descarca_AnexaPublicaAHclPublicat_200()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);

            var bytes = Bytes("%PDF-1.4 anexa publica");
            var up = await admin.IncarcaAnexaHclAsync(
                hcl.HclId, bytes, "anexa.pdf", "Anexa 1", TipDocumentHcl.Anexa, 1);
            var docId = (await up.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
            await admin.SeteazaVizibilitateDocumentAsync(docId, true);
            await admin.PutAsJsonAsync($"/api/Hcl/{hcl.HclId}/Publicare", new { estePublicat = true });

            // branch-ul HclId din PublicDocumente (anexa are SedintaId+PunctId null — fix NPE S51)
            var client = _factory.CreateClient();
            var descarcare = await client.GetAsync($"/public/{inst.Slug}/documente/{docId}");
            Assert.Equal(HttpStatusCode.OK, descarcare.StatusCode);
            Assert.Equal(bytes, await descarcare.Content.ReadAsByteArrayAsync());
        }
    }

    [Fact]
    public async Task Descarca_AnexaAHclNepublicat_404()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);

            var up = await admin.IncarcaAnexaHclAsync(
                hcl.HclId, Bytes("x"), "anexa.pdf", "Anexa 1", TipDocumentHcl.Anexa, 1);
            var docId = (await up.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
            await admin.SeteazaVizibilitateDocumentAsync(docId, true);
            // HCL rămâne nepublicat

            var client = _factory.CreateClient();
            var descarcare = await client.GetAsync($"/public/{inst.Slug}/documente/{docId}");
            Assert.Equal(HttpStatusCode.NotFound, descarcare.StatusCode);
        }
    }

    private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);

    private async Task<(InstitutieDeTest Inst, HttpClient Admin)> InstitutieAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        var admin = await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
        return (inst, admin);
    }
}
