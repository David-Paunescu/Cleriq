using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteVotSiCvorum
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteVotSiCvorum(CleriqFixture fixture) => _factory = fixture.Factory;

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
    public async Task Cvorum_AbsentMotivatSiInactiviiNuConteaza()
    {
        var (admin, sedintaId, c) = await ScenariuAsync(6);
        using (admin)
        {
            var dezactivare = await admin.PutAsJsonAsync($"/api/Consilieri/{c[5]}", new
            {
                numeComplet = "Consilier 06",
                email = (string?)null,
                telefon = (string?)null,
                activ = false
            });
            Assert.Equal(HttpStatusCode.OK, dezactivare.StatusCode);

            await admin.SeteazaPrezentaAsync(sedintaId, c[0], StatusPrezenta.Prezent);
            await admin.SeteazaPrezentaAsync(sedintaId, c[1], StatusPrezenta.Prezent);
            await admin.SeteazaPrezentaAsync(sedintaId, c[2], StatusPrezenta.OnlinePrezent);
            await admin.SeteazaPrezentaAsync(sedintaId, c[3], StatusPrezenta.AbsentMotivat);

            var cvorum = await admin.GetFromJsonAsync<JsonElement>($"/api/Sedinte/{sedintaId}/Cvorum");

            Assert.Equal(5, cvorum.GetProperty("totalConsilieriActivi").GetInt32());
            Assert.Equal(3, cvorum.GetProperty("prezenti").GetInt32());
            Assert.Equal(3, cvorum.GetProperty("cvorumNecesar").GetInt32());
            Assert.True(cvorum.GetProperty("cvorumIntrunit").GetBoolean());
        }
    }

    [Fact]
    public async Task Vot_DoarPrezentiiPotVota()
    {
        var (admin, sedintaId, c) = await ScenariuAsync(3);
        using (admin)
        {
            await admin.SeteazaPrezentaAsync(sedintaId, c[0], StatusPrezenta.Prezent);
            await admin.SeteazaPrezentaAsync(sedintaId, c[1], StatusPrezenta.AbsentMotivat);

            var punctId = await admin.CreeazaPunctAsync(sedintaId, TipMajoritate.Simpla);

            var votPrezent = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi",
                new { consilierId = c[0], optiune = OptiuneVot.Pentru });
            Assert.Equal(HttpStatusCode.OK, votPrezent.StatusCode);

            var votAbsentMotivat = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi",
                new { consilierId = c[1], optiune = OptiuneVot.Pentru });
            Assert.Equal(HttpStatusCode.BadRequest, votAbsentMotivat.StatusCode);

            var votFaraPrezenta = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi",
                new { consilierId = c[2], optiune = OptiuneVot.Pentru });
            Assert.Equal(HttpStatusCode.BadRequest, votFaraPrezenta.StatusCode);
        }
    }

    [Fact]
    public async Task MajoritateSimpla_AbtinerileIntraInNumitor()
    {
        var (admin, sedintaId, c) = await ScenariuAsync(5);
        using (admin)
        {
            foreach (var id in c)
                await admin.SeteazaPrezentaAsync(sedintaId, id, StatusPrezenta.Prezent);

            var punctId = await admin.CreeazaPunctAsync(sedintaId, TipMajoritate.Simpla);

            await admin.VoteazaAsync(sedintaId, punctId, c[0], OptiuneVot.Pentru);
            await admin.VoteazaAsync(sedintaId, punctId, c[1], OptiuneVot.Pentru);
            await admin.VoteazaAsync(sedintaId, punctId, c[2], OptiuneVot.Impotriva);
            await admin.VoteazaAsync(sedintaId, punctId, c[3], OptiuneVot.Abtinere);
            await admin.VoteazaAsync(sedintaId, punctId, c[4], OptiuneVot.Abtinere);

            var rezultat = await admin.InchideVotAsync(sedintaId, punctId);

            Assert.Equal((int)RezultatPunct.Respins, rezultat.GetProperty("rezultat").GetInt32());
            Assert.Equal(3, rezultat.GetProperty("pragNecesar").GetInt32());
            Assert.Equal(5, rezultat.GetProperty("totalVoturiExprimate").GetInt32());
        }
    }

    [Fact]
    public async Task MajoritateAbsoluta_PragulEPeTotalInFunctie()
    {
        var (admin, sedintaId, c) = await ScenariuAsync(5);
        using (admin)
        {
            for (var i = 0; i < 3; i++)
                await admin.SeteazaPrezentaAsync(sedintaId, c[i], StatusPrezenta.Prezent);

            var punct1 = await admin.CreeazaPunctAsync(sedintaId, TipMajoritate.Absoluta, ordine: 1);
            await admin.VoteazaAsync(sedintaId, punct1, c[0], OptiuneVot.Pentru);
            await admin.VoteazaAsync(sedintaId, punct1, c[1], OptiuneVot.Pentru);
            await admin.VoteazaAsync(sedintaId, punct1, c[2], OptiuneVot.Pentru);
            var rezultat1 = await admin.InchideVotAsync(sedintaId, punct1);
            Assert.Equal((int)RezultatPunct.Adoptat, rezultat1.GetProperty("rezultat").GetInt32());
            Assert.Equal(3, rezultat1.GetProperty("pragNecesar").GetInt32());

            var punct2 = await admin.CreeazaPunctAsync(sedintaId, TipMajoritate.Absoluta, ordine: 2);
            await admin.VoteazaAsync(sedintaId, punct2, c[0], OptiuneVot.Pentru);
            await admin.VoteazaAsync(sedintaId, punct2, c[1], OptiuneVot.Pentru);
            await admin.VoteazaAsync(sedintaId, punct2, c[2], OptiuneVot.Abtinere);
            var rezultat2 = await admin.InchideVotAsync(sedintaId, punct2);
            Assert.Equal((int)RezultatPunct.Respins, rezultat2.GetProperty("rezultat").GetInt32());
        }
    }

    [Fact]
    public async Task MajoritateCalificata_PragulEsteDouaTreimiRotunjitInSus()
    {
        var (admin, sedintaId, c) = await ScenariuAsync(5);
        using (admin)
        {
            foreach (var id in c)
                await admin.SeteazaPrezentaAsync(sedintaId, id, StatusPrezenta.Prezent);

            var punct1 = await admin.CreeazaPunctAsync(sedintaId, TipMajoritate.Calificata, ordine: 1);
            for (var i = 0; i < 4; i++)
                await admin.VoteazaAsync(sedintaId, punct1, c[i], OptiuneVot.Pentru);
            await admin.VoteazaAsync(sedintaId, punct1, c[4], OptiuneVot.Abtinere);
            var rezultat1 = await admin.InchideVotAsync(sedintaId, punct1);
            Assert.Equal((int)RezultatPunct.Adoptat, rezultat1.GetProperty("rezultat").GetInt32());
            Assert.Equal(4, rezultat1.GetProperty("pragNecesar").GetInt32());

            var punct2 = await admin.CreeazaPunctAsync(sedintaId, TipMajoritate.Calificata, ordine: 2);
            for (var i = 0; i < 3; i++)
                await admin.VoteazaAsync(sedintaId, punct2, c[i], OptiuneVot.Pentru);
            var rezultat2 = await admin.InchideVotAsync(sedintaId, punct2);
            Assert.Equal((int)RezultatPunct.Respins, rezultat2.GetProperty("rezultat").GetInt32());
        }
    }

    [Fact]
    public async Task PunctInchis_NuMaiAcceptaVoturiSiNuSeReinchide()
    {
        var (admin, sedintaId, c) = await ScenariuAsync(2);
        using (admin)
        {
            await admin.SeteazaPrezentaAsync(sedintaId, c[0], StatusPrezenta.Prezent);
            await admin.SeteazaPrezentaAsync(sedintaId, c[1], StatusPrezenta.Prezent);

            var punctId = await admin.CreeazaPunctAsync(sedintaId, TipMajoritate.Simpla);
            await admin.VoteazaAsync(sedintaId, punctId, c[0], OptiuneVot.Pentru);
            await admin.InchideVotAsync(sedintaId, punctId);

            var votDupaInchidere = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi",
                new { consilierId = c[1], optiune = OptiuneVot.Pentru });
            Assert.Equal(HttpStatusCode.Conflict, votDupaInchidere.StatusCode);

            var inchidereDubla = await admin.PostAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Inchide", null);
            Assert.Equal(HttpStatusCode.Conflict, inchidereDubla.StatusCode);
        }
    }

    [Fact]
    public async Task VotSecret_SelfVoteFunctioneaza_NominaleleNuSeExpun()
    {
        var (admin, sedintaId, c) = await ScenariuAsync(2);
        using (admin)
        {
            await admin.SeteazaPrezentaAsync(sedintaId, c[0], StatusPrezenta.Prezent);
            await admin.SeteazaPrezentaAsync(sedintaId, c[1], StatusPrezenta.Prezent);

            var punctId = await admin.CreeazaPunctAsync(
                sedintaId, TipMajoritate.Simpla, tipVot: TipVot.Secret);

            var votManual = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi",
                new { consilierId = c[0], optiune = OptiuneVot.Pentru });
            Assert.Equal(HttpStatusCode.Conflict, votManual.StatusCode);

            using var consilier1 = await _factory.ClientConsilierAsync(admin, c[0]);
            using var consilier2 = await _factory.ClientConsilierAsync(admin, c[1]);

            var self1 = await consilier1.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi/Self",
                new { optiune = OptiuneVot.Pentru });
            Assert.Equal(HttpStatusCode.OK, self1.StatusCode);

            var self2 = await consilier2.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi/Self",
                new { optiune = OptiuneVot.Impotriva });
            Assert.Equal(HttpStatusCode.OK, self2.StatusCode);

            var voturi = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi");

            Assert.Equal(1, voturi.GetProperty("pentru").GetInt32());
            Assert.Equal(1, voturi.GetProperty("impotriva").GetInt32());
            Assert.Equal(0, voturi.GetProperty("voturiNominale").GetArrayLength());
            Assert.Equal(2, voturi.GetProperty("participanti").GetArrayLength());

            var stergere = await admin.DeleteAsync(
                $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi/{c[0]}");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }
}