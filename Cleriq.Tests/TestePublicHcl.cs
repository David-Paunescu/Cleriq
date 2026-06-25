using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TestePublicHcl
{
    private readonly CleriqWebApplicationFactory _factory;

    public TestePublicHcl(CleriqFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task Lista_DoarHclPublicatSiNumerotat()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            var draft = await admin.GenereazaHclAdoptatAsync();
            var numerotatNepublicat = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(numerotatNepublicat.HclId, 1);
            var publicat = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(publicat.HclId, 2);
            await admin.PutAsJsonAsync($"/api/Hcl/{publicat.HclId}/Publicare", new { estePublicat = true });

            var client = _factory.CreateClient();
            var lista = await client.GetFromJsonAsync<JsonElement>($"/public/{inst.Slug}/hcl");
            var iduri = lista.EnumerateArray().Select(h => h.GetProperty("id").GetInt32()).ToList();
            Assert.Contains(publicat.HclId, iduri);
            Assert.DoesNotContain(draft.HclId, iduri);
            Assert.DoesNotContain(numerotatNepublicat.HclId, iduri);

            Assert.Equal(HttpStatusCode.OK,
                (await client.GetAsync($"/public/{inst.Slug}/hcl/{publicat.HclId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await client.GetAsync($"/public/{inst.Slug}/hcl/{draft.HclId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await client.GetAsync($"/public/{inst.Slug}/hcl/{numerotatNepublicat.HclId}")).StatusCode);
        }
    }

    [Fact]
    public async Task Detalii_ExpuneCampuriPublice_FaraInternals()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            await admin.PutAsJsonAsync($"/api/Hcl/{hcl.HclId}/Publicare", new { estePublicat = true });

            var client = _factory.CreateClient();
            var detalii = await client.GetFromJsonAsync<JsonElement>($"/public/{inst.Slug}/hcl/{hcl.HclId}");

            Assert.False(string.IsNullOrEmpty(detalii.GetProperty("titlu").GetString()));
            Assert.False(string.IsNullOrEmpty(detalii.GetProperty("continut").GetString()));
            Assert.Equal(2, detalii.GetProperty("semnatari").GetArrayLength());
            // internalele NU se expun pe portal
            Assert.False(detalii.TryGetProperty("comunicari", out _));
            Assert.False(detalii.TryGetProperty("caleStocareSemnat", out _));
        }
    }

    [Fact]
    public async Task Pdf_LaNumerotatPublicat_ReturneazaPdfGenerat()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            await admin.PutAsJsonAsync($"/api/Hcl/{hcl.HclId}/Publicare", new { estePublicat = true });

            var client = _factory.CreateClient();
            var raspuns = await client.GetAsync($"/public/{inst.Slug}/hcl/{hcl.HclId}/pdf");
            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
            Assert.Equal("application/pdf", raspuns.Content.Headers.ContentType!.MediaType);
            var bytes = await raspuns.Content.ReadAsByteArrayAsync();
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        }
    }

    [Fact]
    public async Task Pdf_LaSemnat_SeCacheaza()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            await admin.SemneazaHclAsync(hcl.HclId);
            await admin.PutAsJsonAsync($"/api/Hcl/{hcl.HclId}/Publicare", new { estePublicat = true });

            var client = _factory.CreateClient();
            var urlPdf = $"/public/{inst.Slug}/hcl/{hcl.HclId}/pdf";

            var primul = await client.GetAsync(urlPdf);
            Assert.Equal(HttpStatusCode.OK, primul.StatusCode);
            var bytes1 = await primul.Content.ReadAsByteArrayAsync();
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes1, 0, 4));

            var bytes2 = await (await client.GetAsync(urlPdf)).Content.ReadAsByteArrayAsync();
            Assert.Equal(bytes1, bytes2); // egalitate byte-cu-byte = servit din cache (status Semnat)
        }
    }

    [Fact]
    public async Task Pdf_VariantaSemnataArePrioritate_CuFallbackLaGenerat()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            await admin.SemneazaHclAsync(hcl.HclId);

            var bytesSemnat = Encoding.UTF8.GetBytes("%PDF-1.4 hcl semnat extern distinctiv");
            var up = await admin.IncarcaHclSemnatAsync(hcl.HclId, bytesSemnat, "intern-confidential.pdf");
            Assert.Equal(HttpStatusCode.OK, up.StatusCode);
            await admin.PutAsJsonAsync($"/api/Hcl/{hcl.HclId}/Publicare", new { estePublicat = true });

            var client = _factory.CreateClient();
            var urlPdf = $"/public/{inst.Slug}/hcl/{hcl.HclId}/pdf";

            var semnat = await client.GetAsync(urlPdf);
            Assert.Equal(HttpStatusCode.OK, semnat.StatusCode);
            Assert.Equal(bytesSemnat, await semnat.Content.ReadAsByteArrayAsync());
            var dispozitie = semnat.Content.Headers.ContentDisposition!.ToString();
            Assert.Contains("-semnat", dispozitie);                 // nume canonic
            Assert.DoesNotContain("intern-confidential", dispozitie); // nu numele încărcat

            var cheie = await DbTest.CitesteCaleStocareSemnatHclAsync(hcl.HclId);
            Assert.False(string.IsNullOrEmpty(cheie));
            var caleFizica = Path.Combine(AppContext.BaseDirectory, ConfigTest.CaleRootDocumente, cheie!);
            File.Delete(caleFizica);

            var fallback = await client.GetAsync(urlPdf);
            Assert.Equal(HttpStatusCode.OK, fallback.StatusCode);
            var bytesFallback = await fallback.Content.ReadAsByteArrayAsync();
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytesFallback, 0, 4));
            Assert.NotEqual(bytesSemnat, bytesFallback); // degradare grațioasă la PDF generat
        }
    }

    [Fact]
    public async Task Invalidat_RamaneVizibilCuBadge()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            await admin.PutAsJsonAsync($"/api/Hcl/{hcl.HclId}/Publicare", new { estePublicat = true });

            var inv = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Invalidare", new
            {
                motiv = MotivInvalidare.AnulatInstanta,
                refInvalidare = "Sent. civ. 100/2026",
                confirmaCuRelatiiActive = false
            });
            Assert.Equal(HttpStatusCode.OK, inv.StatusCode);

            var client = _factory.CreateClient();

            // rămâne în listă cu flag de invalidare (vizibilitatea persistă — decizie juridică)
            var lista = await client.GetFromJsonAsync<JsonElement>($"/public/{inst.Slug}/hcl");
            var randul = lista.EnumerateArray()
                .First(h => h.GetProperty("id").GetInt32() == hcl.HclId);
            Assert.True(randul.GetProperty("esteInvalidat").GetBoolean());

            var detalii = await client.GetAsync($"/public/{inst.Slug}/hcl/{hcl.HclId}");
            Assert.Equal(HttpStatusCode.OK, detalii.StatusCode);
            var corp = await detalii.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotEqual(JsonValueKind.Null, corp.GetProperty("dataInvalidare").ValueKind);
        }
    }

    private async Task<(InstitutieDeTest Inst, HttpClient Admin)> InstitutieAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        var admin = await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
        return (inst, admin);
    }
}
