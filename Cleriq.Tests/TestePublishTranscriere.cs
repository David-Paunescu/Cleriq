using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TestePublishTranscriere
{
    private readonly CleriqWebApplicationFactory _factory;

    public TestePublishTranscriere(CleriqFixture fixture) => _factory = fixture.Factory;

    private async Task<(HttpClient Admin, int SedintaId)> ScenariuFinalizataAsync(string continutEditat)
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        var admin = await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
        var sedintaId = await admin.CreeazaSedintaAsync();

        var upload = await admin.IncarcaAudioAsync(
            sedintaId, Encoding.UTF8.GetBytes("audio"), "sedinta.mp3");
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var id = (await upload.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();

        await DbTest.SeteazaStatusTranscriereAsync(id, StatusTranscriere.Finalizata,
            """{"text":[{"text":"segment brut"}]}""");

        var setEdit = await admin.PutAsJsonAsync(
            $"/api/Sedinte/{sedintaId}/Transcriere/Continut",
            new { continutEditat });
        Assert.Equal(HttpStatusCode.OK, setEdit.StatusCode);

        return (admin, sedintaId);
    }

    private async Task<HttpClient> ClientSecretarAsync(HttpClient admin)
    {
        var email = $"secretar-{Guid.NewGuid():N}@cleriq.ro";
        const string parola = "TestSecretar1!";
        var register = await admin.PostAsJsonAsync("/api/Auth/register", new
        {
            email,
            parola,
            numeComplet = "Secretar Test",
            rol = "Secretar"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        return await _factory.ClientAutentificatAsync(email, parola);
    }

    [Fact]
    public async Task Publica_PeFinalizataCuEditatNeVid_CopiazaSnapshotSiSeteazaMetadate()
    {
        var (admin, sedintaId) = await ScenariuFinalizataAsync("## Transcript publicat");
        using (admin)
        {
            var publish = await admin.PostAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Publica", null);
            Assert.Equal(HttpStatusCode.OK, publish.StatusCode);

            var dto = await publish.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotEqual(JsonValueKind.Null, dto.GetProperty("dataPublicare").ValueKind);
            Assert.NotEqual(JsonValueKind.Null, dto.GetProperty("publicataDe").ValueKind);

            var continut = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/Transcriere/Continut");
            Assert.Equal("## Transcript publicat",
                continut.GetProperty("continutPublicat").GetString());
        }
    }

    [Fact]
    public async Task Publica_PeStatusDiferitDeFinalizata_Returneaza409()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        var admin = await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
        using (admin)
        {
            var sedintaId = await admin.CreeazaSedintaAsync();
            var upload = await admin.IncarcaAudioAsync(
                sedintaId, Encoding.UTF8.GetBytes("audio"), "sedinta.mp3");
            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
            var id = (await upload.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("id").GetInt32();

            var publishInAsteptare = await admin.PostAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Publica", null);
            Assert.Equal(HttpStatusCode.Conflict, publishInAsteptare.StatusCode);

            await DbTest.SeteazaStatusTranscriereAsync(id, StatusTranscriere.Esuata);
            var publishEsuata = await admin.PostAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Publica", null);
            Assert.Equal(HttpStatusCode.Conflict, publishEsuata.StatusCode);
        }
    }

    [Fact]
    public async Task Publica_PeFinalizataCuEditatVid_Returneaza409()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        var admin = await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
        using (admin)
        {
            var sedintaId = await admin.CreeazaSedintaAsync();
            var upload = await admin.IncarcaAudioAsync(
                sedintaId, Encoding.UTF8.GetBytes("audio"), "sedinta.mp3");
            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
            var id = (await upload.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("id").GetInt32();

            await DbTest.SeteazaStatusTranscriereAsync(id, StatusTranscriere.Finalizata,
                """{"text":[{"text":"brut"}]}""");

            var publishNull = await admin.PostAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Publica", null);
            Assert.Equal(HttpStatusCode.Conflict, publishNull.StatusCode);

            var setEdit = await admin.PutAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Continut",
                new { continutEditat = "   " });
            Assert.Equal(HttpStatusCode.OK, setEdit.StatusCode);

            var publishWs = await admin.PostAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Publica", null);
            Assert.Equal(HttpStatusCode.Conflict, publishWs.StatusCode);
        }
    }

    [Fact]
    public async Task RetragePublicare_AdminNullareazaSnapshot_SecretarPrimeste403()
    {
        var (admin, sedintaId) = await ScenariuFinalizataAsync("## Conținut publicat");
        using (admin)
        {
            var publish = await admin.PostAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Publica", null);
            Assert.Equal(HttpStatusCode.OK, publish.StatusCode);

            using var secretar = await ClientSecretarAsync(admin);
            var retragereSecretar = await secretar.DeleteAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Publica");
            Assert.Equal(HttpStatusCode.Forbidden, retragereSecretar.StatusCode);

            var dupaSecretar = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/Transcriere/Continut");
            Assert.Equal("## Conținut publicat",
                dupaSecretar.GetProperty("continutPublicat").GetString());

            var retragereAdmin = await admin.DeleteAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Publica");
            Assert.Equal(HttpStatusCode.OK, retragereAdmin.StatusCode);

            var dto = await retragereAdmin.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Null, dto.GetProperty("dataPublicare").ValueKind);
            Assert.Equal(JsonValueKind.Null, dto.GetProperty("publicataDe").ValueKind);

            var continut = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/Transcriere/Continut");
            Assert.Equal(JsonValueKind.Null, continut.GetProperty("continutPublicat").ValueKind);
            Assert.Equal("## Conținut publicat",
                continut.GetProperty("continutEditat").GetString());
        }
    }

    [Fact]
    public async Task Republicare_ActualizeazaSnapshotSiDataPublicare()
    {
        var (admin, sedintaId) = await ScenariuFinalizataAsync("v1");
        using (admin)
        {
            var publish1 = await admin.PostAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Publica", null);
            Assert.Equal(HttpStatusCode.OK, publish1.StatusCode);
            var dto1 = await publish1.Content.ReadFromJsonAsync<JsonElement>();
            var t1 = dto1.GetProperty("dataPublicare").GetDateTime();

            var continut1 = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/Transcriere/Continut");
            Assert.Equal("v1", continut1.GetProperty("continutPublicat").GetString());

            await Task.Delay(15);

            var setEdit = await admin.PutAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Continut",
                new { continutEditat = "v2" });
            Assert.Equal(HttpStatusCode.OK, setEdit.StatusCode);

            var continutIntermediar = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/Transcriere/Continut");
            Assert.Equal("v1", continutIntermediar.GetProperty("continutPublicat").GetString());
            Assert.Equal("v2", continutIntermediar.GetProperty("continutEditat").GetString());

            var publish2 = await admin.PostAsync(
                $"/api/Sedinte/{sedintaId}/Transcriere/Publica", null);
            Assert.Equal(HttpStatusCode.OK, publish2.StatusCode);
            var dto2 = await publish2.Content.ReadFromJsonAsync<JsonElement>();
            var t2 = dto2.GetProperty("dataPublicare").GetDateTime();

            Assert.True(t2 > t1);

            var continut2 = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/Transcriere/Continut");
            Assert.Equal("v2", continut2.GetProperty("continutPublicat").GetString());
        }
    }
}