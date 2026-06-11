using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteTranscriere
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteTranscriere(CleriqFixture fixture) => _factory = fixture.Factory;

    private async Task<(HttpClient Admin, int SedintaId)> ScenariuAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        var admin = await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
        var sedintaId = await admin.CreeazaSedintaAsync();
        return (admin, sedintaId);
    }

    [Fact]
    public async Task Upload_CreeazaInAsteptare_SiAudioSeDescarcaIdentic()
    {
        var (admin, sedintaId) = await ScenariuAsync();
        using (admin)
        {
            var bytes = Encoding.UTF8.GetBytes("audio fals pentru testul de upload");
            var upload = await admin.IncarcaAudioAsync(sedintaId, bytes, "sedinta.mp3");
            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

            var dto = await upload.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((int)StatusTranscriere.InAsteptare, dto.GetProperty("status").GetInt32());
            Assert.Equal((long)bytes.Length, dto.GetProperty("dimensiuneAudio").GetInt64());
            Assert.Equal(0, dto.GetProperty("numarIncercari").GetInt32());

            var audio = await admin.GetAsync($"/api/Sedinte/{sedintaId}/Transcriere/Audio");
            Assert.Equal(HttpStatusCode.OK, audio.StatusCode);
            Assert.Equal("audio/mpeg", audio.Content.Headers.ContentType!.MediaType);
            Assert.Equal(bytes, await audio.Content.ReadAsByteArrayAsync());
        }
    }

    [Fact]
    public async Task Upload_ExtensieNepermisaSiFisierGol_Returneaza400()
    {
        var (admin, sedintaId) = await ScenariuAsync();
        using (admin)
        {
            var extensieGresita = await admin.IncarcaAudioAsync(
                sedintaId, Encoding.UTF8.GetBytes("x"), "audio.txt");
            Assert.Equal(HttpStatusCode.BadRequest, extensieGresita.StatusCode);

            var fisierGol = await admin.IncarcaAudioAsync(
                sedintaId, Array.Empty<byte>(), "audio.mp3");
            Assert.Equal(HttpStatusCode.BadRequest, fisierGol.StatusCode);
        }
    }

    [Fact]
    public async Task ReUpload_PeTranscriereActiva_Returneaza409()
    {
        var (admin, sedintaId) = await ScenariuAsync();
        using (admin)
        {
            var primul = await admin.IncarcaAudioAsync(
                sedintaId, Encoding.UTF8.GetBytes("audio unu"), "sedinta.mp3");
            Assert.Equal(HttpStatusCode.OK, primul.StatusCode);

            var alDoilea = await admin.IncarcaAudioAsync(
                sedintaId, Encoding.UTF8.GetBytes("audio doi"), "sedinta.mp3");
            Assert.Equal(HttpStatusCode.Conflict, alDoilea.StatusCode);
        }
    }

    [Fact]
    public async Task ReUpload_PeEsuata_RestaureazaAcelasiRandCuResetComplet()
    {
        var (admin, sedintaId) = await ScenariuAsync();
        using (admin)
        {
            var bytesV1 = Encoding.UTF8.GetBytes("audio versiunea unu");
            var primul = await admin.IncarcaAudioAsync(sedintaId, bytesV1, "sedinta-v1.mp3");
            Assert.Equal(HttpStatusCode.OK, primul.StatusCode);
            var id = (await primul.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("id").GetInt32();

            await DbTest.SeteazaStatusTranscriereAsync(id, StatusTranscriere.Esuata);

            var bytesV2 = Encoding.UTF8.GetBytes("audio versiunea doi, mai lung decat primul");
            var reupload = await admin.IncarcaAudioAsync(sedintaId, bytesV2, "sedinta-v2.mp3");
            Assert.Equal(HttpStatusCode.OK, reupload.StatusCode);

            var dto = await reupload.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(id, dto.GetProperty("id").GetInt32());
            Assert.Equal((int)StatusTranscriere.InAsteptare, dto.GetProperty("status").GetInt32());
            Assert.Equal(0, dto.GetProperty("numarIncercari").GetInt32());
            Assert.Equal(JsonValueKind.Null, dto.GetProperty("ultimaEroare").ValueKind);
            Assert.Equal((long)bytesV2.Length, dto.GetProperty("dimensiuneAudio").GetInt64());

            var audio = await admin.GetAsync($"/api/Sedinte/{sedintaId}/Transcriere/Audio");
            Assert.Equal(bytesV2, await audio.Content.ReadAsByteArrayAsync());
        }
    }

    [Fact]
    public async Task ReUpload_DupaStergere_RestaureazaAcelasiRand()
    {
        var (admin, sedintaId) = await ScenariuAsync();
        using (admin)
        {
            var primul = await admin.IncarcaAudioAsync(
                sedintaId, Encoding.UTF8.GetBytes("audio initial"), "sedinta.mp3");
            Assert.Equal(HttpStatusCode.OK, primul.StatusCode);
            var id = (await primul.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("id").GetInt32();

            var stergere = await admin.DeleteAsync($"/api/Sedinte/{sedintaId}/Transcriere");
            Assert.Equal(HttpStatusCode.NoContent, stergere.StatusCode);

            var dupaStergere = await admin.GetAsync($"/api/Sedinte/{sedintaId}/Transcriere");
            Assert.Equal(HttpStatusCode.NotFound, dupaStergere.StatusCode);

            var reupload = await admin.IncarcaAudioAsync(
                sedintaId, Encoding.UTF8.GetBytes("audio nou dupa restore"), "sedinta-nou.mp3");
            Assert.Equal(HttpStatusCode.OK, reupload.StatusCode);

            var dto = await reupload.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(id, dto.GetProperty("id").GetInt32());
            Assert.Equal((int)StatusTranscriere.InAsteptare, dto.GetProperty("status").GetInt32());
        }
    }

    [Fact]
    public async Task Retry_PeEsuata_ReseteazaContorulSiEroarea()
    {
        var (admin, sedintaId) = await ScenariuAsync();
        using (admin)
        {
            var upload = await admin.IncarcaAudioAsync(
                sedintaId, Encoding.UTF8.GetBytes("audio"), "sedinta.mp3");
            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
            var id = (await upload.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("id").GetInt32();

            await DbTest.SeteazaStatusTranscriereAsync(id, StatusTranscriere.Esuata);

            var retry = await admin.PostAsync($"/api/Sedinte/{sedintaId}/Transcriere/Retry", null);
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);

            var dto = await retry.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((int)StatusTranscriere.InAsteptare, dto.GetProperty("status").GetInt32());
            Assert.Equal(0, dto.GetProperty("numarIncercari").GetInt32());
            Assert.Equal(JsonValueKind.Null, dto.GetProperty("ultimaEroare").ValueKind);
            Assert.Equal(JsonValueKind.Null, dto.GetProperty("urmatoareaIncercareDupa").ValueKind);
        }
    }

    [Fact]
    public async Task Retry_PeAlteStatusuri_Returneaza409()
    {
        var (admin, sedintaId) = await ScenariuAsync();
        using (admin)
        {
            var upload = await admin.IncarcaAudioAsync(
                sedintaId, Encoding.UTF8.GetBytes("audio"), "sedinta.mp3");
            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
            var id = (await upload.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("id").GetInt32();

            var retryInAsteptare = await admin.PostAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Retry", null);
            Assert.Equal(HttpStatusCode.Conflict, retryInAsteptare.StatusCode);

            await DbTest.SeteazaStatusTranscriereAsync(id, StatusTranscriere.Finalizata);

            var retryFinalizata = await admin.PostAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Retry", null);
            Assert.Equal(HttpStatusCode.Conflict, retryFinalizata.StatusCode);
        }
    }

    [Fact]
    public async Task EditareContinut_DoarPeFinalizata_IarBrutulRamaneIntact()
    {
        var (admin, sedintaId) = await ScenariuAsync();
        using (admin)
        {
            var upload = await admin.IncarcaAudioAsync(
                sedintaId, Encoding.UTF8.GetBytes("audio"), "sedinta.mp3");
            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
            var id = (await upload.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("id").GetInt32();

            var editarePrematura = await admin.PutAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Continut",
                new { continutEditat = "prea devreme" });
            Assert.Equal(HttpStatusCode.Conflict, editarePrematura.StatusCode);

            const string brut = """{"text": [{"text": "segment brut imutabil"}]}""";
            await DbTest.SeteazaStatusTranscriereAsync(id, StatusTranscriere.Finalizata, brut);

            const string editat = "## Transcript editat de secretar";
            var editare = await admin.PutAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Continut",
                new { continutEditat = editat });
            Assert.Equal(HttpStatusCode.OK, editare.StatusCode);

            var dtoEditare = await editare.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotEqual(JsonValueKind.Null, dtoEditare.GetProperty("dataUltimeiEditari").ValueKind);

            var continut = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/Transcriere/Continut");
            Assert.Equal(brut, continut.GetProperty("continutBrut").GetString());
            Assert.Equal(editat, continut.GetProperty("continutEditat").GetString());
        }
    }

    [Fact]
    public async Task OperatiiPeSedintaFaraTranscriere_Returneaza404()
    {
        var (admin, sedintaId) = await ScenariuAsync();
        using (admin)
        {
            var detalii = await admin.GetAsync($"/api/Sedinte/{sedintaId}/Transcriere");
            Assert.Equal(HttpStatusCode.NotFound, detalii.StatusCode);

            var continut = await admin.GetAsync($"/api/Sedinte/{sedintaId}/Transcriere/Continut");
            Assert.Equal(HttpStatusCode.NotFound, continut.StatusCode);

            var audio = await admin.GetAsync($"/api/Sedinte/{sedintaId}/Transcriere/Audio");
            Assert.Equal(HttpStatusCode.NotFound, audio.StatusCode);

            var retry = await admin.PostAsync($"/api/Sedinte/{sedintaId}/Transcriere/Retry", null);
            Assert.Equal(HttpStatusCode.NotFound, retry.StatusCode);

            var editare = await admin.PutAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Continut", new { continutEditat = "x" });
            Assert.Equal(HttpStatusCode.NotFound, editare.StatusCode);
        }
    }
}