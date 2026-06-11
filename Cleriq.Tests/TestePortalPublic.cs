using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cleriq.DTOs;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TestePortalPublic
{
    private readonly CleriqWebApplicationFactory _factory;

    public TestePortalPublic(CleriqFixture fixture) => _factory = fixture.Factory;

    private async Task<(InstitutieDeTest Inst, HttpClient Admin)> InstitutieAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        var admin = await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
        return (inst, admin);
    }

    [Fact]
    public async Task Sedinte_DoarCeleConvocateSauUlterioareSuntPublice()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            await admin.CreeazaConsilierAsync("Consilier Unu");
            var planificataId = await admin.CreeazaSedintaAsync("Ședință Planificată");
            var convocataId = await admin.CreeazaSedintaAsync("Ședință Convocată");
            var anulataId = await admin.CreeazaSedintaAsync("Ședință Anulată");

            await admin.CreeazaPunctAsync(convocataId, TipMajoritate.Simpla);
            await admin.TrimiteConvocariAsync(convocataId);
            await admin.TranziteazaSedintaAsync(anulataId, "Anuleaza");

            var client = _factory.CreateClient();

            var lista = await client.GetFromJsonAsync<JsonElement>($"/public/{inst.Slug}/sedinte");
            var iduri = lista.EnumerateArray().Select(s => s.GetProperty("id").GetInt32()).ToList();
            Assert.Contains(convocataId, iduri);
            Assert.DoesNotContain(planificataId, iduri);
            Assert.DoesNotContain(anulataId, iduri);

            Assert.Equal(HttpStatusCode.NotFound,
                (await client.GetAsync($"/public/{inst.Slug}/sedinte/{planificataId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await client.GetAsync($"/public/{inst.Slug}/sedinte/{anulataId}")).StatusCode);

            var detalii = await client.GetAsync($"/public/{inst.Slug}/sedinte/{convocataId}");
            Assert.Equal(HttpStatusCode.OK, detalii.StatusCode);
            var corp = await detalii.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, corp.GetProperty("ordineDeZi").GetArrayLength());
        }
    }

    [Fact]
    public async Task Voturi_NominaleCuNume_SecreteDoarTally()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            var c0 = await admin.CreeazaConsilierAsync("Consilier 01");
            var c1 = await admin.CreeazaConsilierAsync("Consilier 02");
            var sedintaId = await admin.CreeazaSedintaAsync();
            await admin.SeteazaPrezentaAsync(sedintaId, c0, StatusPrezenta.Prezent);
            await admin.SeteazaPrezentaAsync(sedintaId, c1, StatusPrezenta.Prezent);
            await admin.TrimiteConvocariAsync(sedintaId);

            var nominalId = await admin.CreeazaPunctAsync(sedintaId, TipMajoritate.Simpla, ordine: 1);
            await admin.VoteazaAsync(sedintaId, nominalId, c0, OptiuneVot.Pentru);
            await admin.VoteazaAsync(sedintaId, nominalId, c1, OptiuneVot.Impotriva);

            var secretId = await admin.CreeazaPunctAsync(
                sedintaId, TipMajoritate.Simpla, ordine: 2, tipVot: TipVot.Secret);
            using var consilier1 = await _factory.ClientConsilierAsync(admin, c0);
            using var consilier2 = await _factory.ClientConsilierAsync(admin, c1);
            var v1 = await consilier1.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{secretId}/Voturi/Self",
                new { optiune = OptiuneVot.Pentru });
            Assert.Equal(HttpStatusCode.OK, v1.StatusCode);
            var v2 = await consilier2.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{secretId}/Voturi/Self",
                new { optiune = OptiuneVot.Pentru });
            Assert.Equal(HttpStatusCode.OK, v2.StatusCode);

            var client = _factory.CreateClient();
            var voturi = await client.GetFromJsonAsync<JsonElement>(
                $"/public/{inst.Slug}/sedinte/{sedintaId}/voturi");
            Assert.Equal(2, voturi.GetArrayLength());

            var nominal = voturi[0];
            Assert.Equal(1, nominal.GetProperty("pentru").GetInt32());
            Assert.Equal(1, nominal.GetProperty("impotriva").GetInt32());
            Assert.Equal(2, nominal.GetProperty("voturiNominale").GetArrayLength());
            var numePentru = nominal.GetProperty("voturiNominale").EnumerateArray()
                .First(v => v.GetProperty("optiune").GetInt32() == (int)OptiuneVot.Pentru)
                .GetProperty("numeCompletConsilier").GetString();
            Assert.Equal("Consilier 01", numePentru);

            var secret = voturi[1];
            Assert.Equal((int)TipVot.Secret, secret.GetProperty("tipVot").GetInt32());
            Assert.Equal(2, secret.GetProperty("pentru").GetInt32());
            Assert.Equal(0, secret.GetProperty("voturiNominale").GetArrayLength());
        }
    }

    [Fact]
    public async Task ProcesVerbal_PublicDoarFinalizat_SiDoarPeSedintaPublica()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            await admin.CreeazaConsilierAsync("Consilier Unu");
            var sedintaId = await admin.CreeazaSedintaAsync();
            await admin.TrimiteConvocariAsync(sedintaId);
            await admin.GenereazaProcesVerbalAsync(sedintaId);

            var client = _factory.CreateClient();
            var urlPv = $"/public/{inst.Slug}/sedinte/{sedintaId}/procesverbal";

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(urlPv)).StatusCode);

            await admin.FinalizeazaProcesVerbalAsync(sedintaId);

            var raspuns = await client.GetAsync(urlPv);
            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
            var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(corp.GetProperty("esteSemnat").GetBoolean());
            Assert.Contains("# Proces verbal", corp.GetProperty("continut").GetString());

            var markdown = await client.GetAsync($"{urlPv}/markdown");
            Assert.Equal(HttpStatusCode.OK, markdown.StatusCode);
            Assert.Equal("text/markdown", markdown.Content.Headers.ContentType!.MediaType);

            var retrogradare = await admin.DeleteAsync($"/api/Sedinte/{sedintaId}/Convocare");
            Assert.Equal(HttpStatusCode.NoContent, retrogradare.StatusCode);

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(urlPv)).StatusCode);
        }
    }

    [Fact]
    public async Task PdfPublic_SeCacheaza_DarVizibilitateaRamanePerRequest()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            await admin.CreeazaConsilierAsync("Consilier Unu");
            var sedintaId = await admin.CreeazaSedintaAsync();
            await admin.TrimiteConvocariAsync(sedintaId);
            await admin.GenereazaProcesVerbalAsync(sedintaId);
            await admin.FinalizeazaProcesVerbalAsync(sedintaId);

            var client = _factory.CreateClient();
            var urlPdf = $"/public/{inst.Slug}/sedinte/{sedintaId}/procesverbal/pdf";

            var primul = await client.GetAsync(urlPdf);
            Assert.Equal(HttpStatusCode.OK, primul.StatusCode);
            Assert.Equal("application/pdf", primul.Content.Headers.ContentType!.MediaType);
            var bytes1 = await primul.Content.ReadAsByteArrayAsync();
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes1, 0, 4));

            var alDoilea = await client.GetAsync(urlPdf);
            var bytes2 = await alDoilea.Content.ReadAsByteArrayAsync();
            Assert.Equal(bytes1, bytes2);

            var stergere = await admin.DeleteAsync($"/api/Sedinte/{sedintaId}");
            Assert.Equal(HttpStatusCode.NoContent, stergere.StatusCode);

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(urlPdf)).StatusCode);
        }
    }

    [Fact]
    public async Task PdfPublic_VariantaSemnataArePrioritate_CuFallbackLaGenerat()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            await admin.CreeazaConsilierAsync("Consilier Unu");
            var sedintaId = await admin.CreeazaSedintaAsync();
            await admin.TrimiteConvocariAsync(sedintaId);
            await admin.GenereazaProcesVerbalAsync(sedintaId);
            await admin.FinalizeazaProcesVerbalAsync(sedintaId);

            var bytesSemnat = Encoding.UTF8.GetBytes("%PDF-1.4 varianta semnata distinctiva");
            var upload = await admin.IncarcaPvSemnatAsync(sedintaId, bytesSemnat, "intern-confidential.pdf");
            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

            var client = _factory.CreateClient();
            var urlPdf = $"/public/{inst.Slug}/sedinte/{sedintaId}/procesverbal/pdf";

            var pv = await client.GetFromJsonAsync<JsonElement>(
                $"/public/{inst.Slug}/sedinte/{sedintaId}/procesverbal");
            Assert.True(pv.GetProperty("esteSemnat").GetBoolean());

            var semnat = await client.GetAsync(urlPdf);
            Assert.Equal(HttpStatusCode.OK, semnat.StatusCode);
            Assert.Equal(bytesSemnat, await semnat.Content.ReadAsByteArrayAsync());
            var dispozitie = semnat.Content.Headers.ContentDisposition!.ToString();
            Assert.Contains("-semnat", dispozitie);
            Assert.DoesNotContain("intern-confidential", dispozitie);

            var cheie = await DbTest.CitesteCaleStocareSemnatAsync(sedintaId);
            Assert.False(string.IsNullOrEmpty(cheie));
            var caleFizica = Path.Combine(AppContext.BaseDirectory, ConfigTest.CaleRootDocumente, cheie!);
            File.Delete(caleFizica);

            var fallback = await client.GetAsync(urlPdf);
            Assert.Equal(HttpStatusCode.OK, fallback.StatusCode);
            var bytesFallback = await fallback.Content.ReadAsByteArrayAsync();
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytesFallback, 0, 4));
            Assert.NotEqual(bytesSemnat, bytesFallback);
        }
    }

    [Fact]
    public async Task DocumentePublice_DublaValidare_SiRetrogradareaLeAscunde()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            await admin.CreeazaConsilierAsync("Consilier Unu");
            var sedintaId = await admin.CreeazaSedintaAsync();
            await admin.TrimiteConvocariAsync(sedintaId);

            var bytes = Encoding.UTF8.GetBytes("%PDF-1.4 continut document public");
            var doc = await admin.IncarcaDocumentAsync(bytes, "anexa.pdf", "Anexă ședință", sedintaId: sedintaId);
            var docId = doc.GetProperty("id").GetInt32();
            Assert.False(doc.GetProperty("estePublic").GetBoolean());

            var client = _factory.CreateClient();
            var urlLista = $"/public/{inst.Slug}/sedinte/{sedintaId}/documente";
            var urlDescarcare = $"/public/{inst.Slug}/documente/{docId}";

            var listaPrivat = await client.GetFromJsonAsync<JsonElement>(urlLista);
            Assert.Equal(0, listaPrivat.GetArrayLength());
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(urlDescarcare)).StatusCode);

            await admin.SeteazaVizibilitateDocumentAsync(docId, true);

            var listaPublic = await client.GetFromJsonAsync<JsonElement>(urlLista);
            Assert.Equal(1, listaPublic.GetArrayLength());
            var descarcare = await client.GetAsync(urlDescarcare);
            Assert.Equal(HttpStatusCode.OK, descarcare.StatusCode);
            Assert.Equal(bytes, await descarcare.Content.ReadAsByteArrayAsync());

            var retrogradare = await admin.DeleteAsync($"/api/Sedinte/{sedintaId}/Convocare");
            Assert.Equal(HttpStatusCode.NoContent, retrogradare.StatusCode);

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(urlLista)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(urlDescarcare)).StatusCode);
        }
    }

    [Fact]
    public async Task DocumentePublice_AtasateLaPunct_ValidatePrinSedinta()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            await admin.CreeazaConsilierAsync("Consilier Unu");
            var sedintaId = await admin.CreeazaSedintaAsync();
            var punctId = await admin.CreeazaPunctAsync(sedintaId, TipMajoritate.Simpla);
            await admin.TrimiteConvocariAsync(sedintaId);

            var bytes = Encoding.UTF8.GetBytes("%PDF-1.4 raport pentru punct");
            var doc = await admin.IncarcaDocumentAsync(bytes, "raport.pdf", "Raport punct", punctId: punctId);
            var docId = doc.GetProperty("id").GetInt32();
            await admin.SeteazaVizibilitateDocumentAsync(docId, true);

            var client = _factory.CreateClient();
            var lista = await client.GetFromJsonAsync<JsonElement>(
                $"/public/{inst.Slug}/puncte/{punctId}/documente");
            Assert.Equal(1, lista.GetArrayLength());

            var descarcare = await client.GetAsync($"/public/{inst.Slug}/documente/{docId}");
            Assert.Equal(HttpStatusCode.OK, descarcare.StatusCode);
            Assert.Equal(bytes, await descarcare.Content.ReadAsByteArrayAsync());
        }
    }

    [Fact]
    public async Task ConvocariPublice_DoarNumeSiStatusAgregat()
    {
        var (inst, admin) = await InstitutieAsync();
        using (admin)
        {
            await admin.CreeazaConsilierAsync("Ana CuEmail", "ana@test.ro");
            await admin.CreeazaConsilierAsync("Costel FaraNimic");
            var sedintaId = await admin.CreeazaSedintaAsync();
            await admin.TrimiteConvocariAsync(sedintaId);

            var client = _factory.CreateClient();
            var lista = await client.GetFromJsonAsync<JsonElement>(
                $"/public/{inst.Slug}/sedinte/{sedintaId}/convocari");
            Assert.Equal(2, lista.GetArrayLength());

            var ana = lista.EnumerateArray()
                .First(c => c.GetProperty("numeCompletConsilier").GetString() == "Ana CuEmail");
            Assert.Equal((int)StatusConvocare.InCursDeTrimitere, ana.GetProperty("status").GetInt32());

            var costel = lista.EnumerateArray()
                .First(c => c.GetProperty("numeCompletConsilier").GetString() == "Costel FaraNimic");
            Assert.Equal((int)StatusConvocare.FaraCoordonate, costel.GetProperty("status").GetInt32());

            Assert.False(ana.TryGetProperty("emailStatus", out _));
            Assert.False(ana.TryGetProperty("smsStatus", out _));
        }
    }
}