using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteSemnatariHcl
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteSemnatariHcl(CleriqFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task Lista_DupaGenerare_DoiSemnatariInitiali()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();

            var lista = await admin.GetFromJsonAsync<JsonElement>($"/api/Hcl/{hcl.HclId}/Semnatari");
            var arr = lista.EnumerateArray().ToList();
            Assert.Equal(2, arr.Count);
            // ordonați după OrdineAfisare: președinte (1), apoi secretar (2)
            Assert.Equal((int)RolSemnatar.PresedinteSedinta, arr[0].GetProperty("rolSemnatar").GetInt32());
            Assert.Equal((int)RolSemnatar.SecretarUat, arr[1].GetProperty("rolSemnatar").GetInt32());
        }
    }

    [Fact]
    public async Task Adauga_AmbelePersoanaSiConsilier_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var persoana = await admin.CreeazaPersoanaAsync("Persoană X");
            var consilier = await admin.CreeazaConsilierAsync("Consilier Y");

            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Semnatari", new
            {
                rol = RolSemnatar.SemnatarAlternativArt140,
                consilierId = consilier,
                persoanaId = persoana,
                ordineAfisare = 3
            });
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Adauga_SecretarUatCaConsilier_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var consilier = await admin.CreeazaConsilierAsync("Consilier Y");

            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Semnatari", new
            {
                rol = RolSemnatar.SecretarUat,
                consilierId = consilier,
                persoanaId = (int?)null,
                ordineAfisare = 3
            });
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Adauga_Art140_FaraMotiv_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var consilier = await admin.CreeazaConsilierAsync("Consilier Alternativ");

            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Semnatari", new
            {
                rol = RolSemnatar.SemnatarAlternativArt140,
                consilierId = consilier,
                persoanaId = (int?)null,
                ordineAfisare = 3
            });
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Adauga_Art140_NeprezentLaSedinta_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await SeteazaMotivAsync(admin, hcl.HclId);
            var consilier = await admin.CreeazaConsilierAsync("Consilier Neprezent");
            // intenționat fără prezență pe ședință

            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Semnatari", new
            {
                rol = RolSemnatar.SemnatarAlternativArt140,
                consilierId = consilier,
                persoanaId = (int?)null,
                ordineAfisare = 3
            });
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Adauga_Art140_CuMotivSiPrezent_200()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await SeteazaMotivAsync(admin, hcl.HclId);
            var consilier = await admin.CreeazaConsilierAsync("Consilier Alternativ");
            await admin.SeteazaPrezentaAsync(hcl.SedintaId, consilier, StatusPrezenta.Prezent);

            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Semnatari", new
            {
                rol = RolSemnatar.SemnatarAlternativArt140,
                consilierId = consilier,
                persoanaId = (int?)null,
                ordineAfisare = 3
            });
            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
            // POST întoarce HclDetaliiDto: lista de semnatari conține acum și alternativul art.140
            var detalii = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
            var semnatari = detalii.GetProperty("semnatari").EnumerateArray().ToList();
            Assert.Equal(3, semnatari.Count);
            Assert.Contains(semnatari, s =>
                s.GetProperty("rolSemnatar").GetInt32() == (int)RolSemnatar.SemnatarAlternativArt140
                && s.GetProperty("consilierId").GetInt32() == consilier);
        }
    }

    [Fact]
    public async Task Adauga_AlDoileaSecretarUat_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var persoana = await admin.CreeazaPersoanaAsync("Secretar Doi");

            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Semnatari", new
            {
                rol = RolSemnatar.SecretarUat,
                consilierId = (int?)null,
                persoanaId = persoana,
                ordineAfisare = 3
            });
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Adauga_ConsilierCuRolExistent_CrossRol_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await SeteazaMotivAsync(admin, hcl.HclId);

            // consilierul-președinte (deja semnatar) e prezent → ajunge la garda cross-rol
            var raspuns = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Semnatari", new
            {
                rol = RolSemnatar.SemnatarAlternativArt140,
                consilierId = hcl.ConsilierId,
                persoanaId = (int?)null,
                ordineAfisare = 3
            });
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Sterge_UltimulArt140_AutoClearMotiv()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await SeteazaMotivAsync(admin, hcl.HclId);
            var consilier = await admin.CreeazaConsilierAsync("Consilier Alternativ");
            await admin.SeteazaPrezentaAsync(hcl.SedintaId, consilier, StatusPrezenta.Prezent);

            var adauga = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Semnatari", new
            {
                rol = RolSemnatar.SemnatarAlternativArt140,
                consilierId = consilier,
                persoanaId = (int?)null,
                ordineAfisare = 3
            });
            Assert.Equal(HttpStatusCode.OK, adauga.StatusCode);
            // POST întoarce Detalii → găsim id-ul alternativului din lista de semnatari
            var detaliiAdaugare = await adauga.Content.ReadFromJsonAsync<JsonElement>();
            var semnatarId = detaliiAdaugare.GetProperty("semnatari").EnumerateArray()
                .Single(s => s.GetProperty("rolSemnatar").GetInt32() == (int)RolSemnatar.SemnatarAlternativArt140)
                .GetProperty("id").GetInt32();

            var stergere = await admin.DeleteAsync($"/api/Hcl/{hcl.HclId}/Semnatari/{semnatarId}");
            Assert.Equal(HttpStatusCode.OK, stergere.StatusCode);

            // DELETE întoarce Detalii: motivul s-a auto-curățat + alternativul a dispărut din listă
            var detalii = await stergere.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Null, detalii.GetProperty("motivLipsaSemnaturaPresedinte").ValueKind);
            Assert.DoesNotContain(detalii.GetProperty("semnatari").EnumerateArray(),
                s => s.GetProperty("rolSemnatar").GetInt32() == (int)RolSemnatar.SemnatarAlternativArt140);
        }
    }

    [Fact]
    public async Task PeHclSemnat_AdaugareSiStergere_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            await admin.SemneazaHclAsync(hcl.HclId);

            var consilier = await admin.CreeazaConsilierAsync("Consilier Y");
            var adauga = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Semnatari", new
            {
                rol = RolSemnatar.SemnatarAlternativArt140,
                consilierId = consilier,
                persoanaId = (int?)null,
                ordineAfisare = 3
            });
            Assert.Equal(HttpStatusCode.Conflict, adauga.StatusCode);

            var lista = await admin.GetFromJsonAsync<JsonElement>($"/api/Hcl/{hcl.HclId}/Semnatari");
            var unSemnatarId = lista.EnumerateArray().First().GetProperty("id").GetInt32();
            var stergere = await admin.DeleteAsync($"/api/Hcl/{hcl.HclId}/Semnatari/{unSemnatarId}");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    private static async Task SeteazaMotivAsync(HttpClient admin, int hclId)
    {
        var raspuns = await admin.PutAsJsonAsync($"/api/Hcl/{hclId}/MotivLipsaPresedinte",
            new { motiv = "Președintele a părăsit ședința înainte de semnare." });
        Assert.True(raspuns.IsSuccessStatusCode, await raspuns.Content.ReadAsStringAsync());
    }

    private async Task<HttpClient> AdminNouAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }
}
