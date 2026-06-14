using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteVotulMeu
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteVotulMeu(CleriqFixture fixture) => _factory = fixture.Factory;

    private async Task<(HttpClient Admin, int SedintaId, List<int> Consilieri)> ScenariuAsync(int nrConsilieri)
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        var admin = await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
        var consilieri = new List<int>();
        for (var i = 1; i <= nrConsilieri; i++)
            consilieri.Add(await admin.CreeazaConsilierAsync($"Consilier {i:D2}"));
        var sedintaId = await admin.CreeazaSedintaAsync();
        return (admin, sedintaId, consilieri);
    }

    [Fact]
    public async Task VotSecret_GetDeVotant_VedeOptiuneaProprie_FaraNominale()
    {
        var (admin, sedintaId, c) = await ScenariuAsync(2);
        using (admin)
        {
            await admin.SeteazaPrezentaAsync(sedintaId, c[0], StatusPrezenta.Prezent);
            await admin.SeteazaPrezentaAsync(sedintaId, c[1], StatusPrezenta.Prezent);

            var punctId = await admin.CreeazaPunctAsync(
                sedintaId, TipMajoritate.Simpla, tipVot: TipVot.Secret);

            using var consilier1 = await _factory.ClientConsilierAsync(admin, c[0]);

            var vot = await consilier1.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi/Self",
                new { optiune = OptiuneVot.Pentru });
            Assert.Equal(HttpStatusCode.OK, vot.StatusCode);

            var voturi = await consilier1.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi");

            Assert.Equal((int)OptiuneVot.Pentru, voturi.GetProperty("votulMeu").GetInt32());
            Assert.Equal(0, voturi.GetProperty("voturiNominale").GetArrayLength());
        }
    }

    [Fact]
    public async Task VotSecret_GetDeConsilierCareNuAVotat_VotulMeuEsteNull()
    {
        var (admin, sedintaId, c) = await ScenariuAsync(2);
        using (admin)
        {
            await admin.SeteazaPrezentaAsync(sedintaId, c[0], StatusPrezenta.Prezent);
            await admin.SeteazaPrezentaAsync(sedintaId, c[1], StatusPrezenta.Prezent);

            var punctId = await admin.CreeazaPunctAsync(
                sedintaId, TipMajoritate.Simpla, tipVot: TipVot.Secret);

            using var consilier1 = await _factory.ClientConsilierAsync(admin, c[0]);
            using var consilier2 = await _factory.ClientConsilierAsync(admin, c[1]);

            var vot = await consilier1.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi/Self",
                new { optiune = OptiuneVot.Pentru });
            Assert.Equal(HttpStatusCode.OK, vot.StatusCode);

            // Consilier2 e prezent dar nu a votat — nu trebuie să vadă votul lui Consilier1
            var voturi = await consilier2.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi");

            Assert.Equal(JsonValueKind.Null, voturi.GetProperty("votulMeu").ValueKind);
            Assert.Equal(0, voturi.GetProperty("voturiNominale").GetArrayLength());
        }
    }

    [Fact]
    public async Task VotSecret_GetDeAdmin_VotulMeuEsteNull()
    {
        var (admin, sedintaId, c) = await ScenariuAsync(2);
        using (admin)
        {
            await admin.SeteazaPrezentaAsync(sedintaId, c[0], StatusPrezenta.Prezent);
            await admin.SeteazaPrezentaAsync(sedintaId, c[1], StatusPrezenta.Prezent);

            var punctId = await admin.CreeazaPunctAsync(
                sedintaId, TipMajoritate.Simpla, tipVot: TipVot.Secret);

            using var consilier1 = await _factory.ClientConsilierAsync(admin, c[0]);
            using var consilier2 = await _factory.ClientConsilierAsync(admin, c[1]);

            var v1 = await consilier1.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi/Self",
                new { optiune = OptiuneVot.Pentru });
            Assert.Equal(HttpStatusCode.OK, v1.StatusCode);

            var v2 = await consilier2.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi/Self",
                new { optiune = OptiuneVot.Impotriva });
            Assert.Equal(HttpStatusCode.OK, v2.StatusCode);

            // Admin nu are claim ConsilierId — votulMeu trebuie să fie null,
            // tally-ul rămâne vizibil pentru contextul administrativ
            var voturi = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi");

            Assert.Equal(JsonValueKind.Null, voturi.GetProperty("votulMeu").ValueKind);
            Assert.Equal(1, voturi.GetProperty("pentru").GetInt32());
            Assert.Equal(1, voturi.GetProperty("impotriva").GetInt32());
        }
    }

    [Fact]
    public async Task VotNominal_GetDeVotant_VotulMeuPopulat_AlaturiDeNominale()
    {
        var (admin, sedintaId, c) = await ScenariuAsync(2);
        using (admin)
        {
            await admin.SeteazaPrezentaAsync(sedintaId, c[0], StatusPrezenta.Prezent);
            await admin.SeteazaPrezentaAsync(sedintaId, c[1], StatusPrezenta.Prezent);

            var punctId = await admin.CreeazaPunctAsync(sedintaId, TipMajoritate.Simpla);

            await admin.VoteazaAsync(sedintaId, punctId, c[0], OptiuneVot.Pentru);
            await admin.VoteazaAsync(sedintaId, punctId, c[1], OptiuneVot.Impotriva);

            using var consilier1 = await _factory.ClientConsilierAsync(admin, c[0]);

            // La nominal, VotulMeu și VoturiNominale coexistă — paritate cu secret
            var voturi = await consilier1.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi");

            Assert.Equal((int)OptiuneVot.Pentru, voturi.GetProperty("votulMeu").GetInt32());
            Assert.Equal(2, voturi.GetProperty("voturiNominale").GetArrayLength());
        }
    }
}