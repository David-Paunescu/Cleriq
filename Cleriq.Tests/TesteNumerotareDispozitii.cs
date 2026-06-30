using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

// Paritar TesteNumerotareHcl: aceeași mecanică (compare-and-swap anti-lacună + numere arse), dar pe
// registrul propriu de dispoziții, prin serviciul generic ServiciuNumerotareActe cu T=Dispozitie.
[Collection("Cleriq")]
public class TesteNumerotareDispozitii
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteNumerotareDispozitii(CleriqFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task AtribuieNumar_PeDraft_Numerotat_SiInlocuiestePlaceholderul()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();

            var raspuns = await admin.PostAsJsonAsync($"/api/Dispozitii/{id}/AtribuieNumar",
                new { numar = 1, confirmaCuLacune = false });
            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

            var detalii = await admin.GetFromJsonAsync<JsonElement>($"/api/Dispozitii/{id}");
            Assert.Equal((int)StatusActRedactional.Numerotat, detalii.GetProperty("status").GetInt32());
            var numar = detalii.GetProperty("numar").GetInt32();
            var an = detalii.GetProperty("anNumerotare").GetInt32();
            Assert.Equal(1, numar);

            // placeholderul din titlu e înlocuit cu numărul real (partajat cu HCL prin PlaceholderAct)
            var continut = detalii.GetProperty("continut").GetString()!;
            Assert.Contains($"{numar}/{an}", continut);
            Assert.DoesNotContain("_[urmează să fie atribuit]_", continut);
        }
    }

    [Fact]
    public async Task AtribuieNumar_NumarZero_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            var raspuns = await admin.PostAsJsonAsync($"/api/Dispozitii/{id}/AtribuieNumar",
                new { numar = 0, confirmaCuLacune = false });
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task AtribuieNumar_PeDispozitieDejaNumerotata_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id, 1);

            var raspuns = await admin.PostAsJsonAsync($"/api/Dispozitii/{id}/AtribuieNumar",
                new { numar = 2, confirmaCuLacune = false });
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task AtribuieNumar_LacuneNeconfirmate_409CuLacune_ApoiConfirmat_200()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();

            var faraConfirm = await admin.PostAsJsonAsync($"/api/Dispozitii/{id}/AtribuieNumar",
                new { numar = 5, confirmaCuLacune = false });
            Assert.Equal(HttpStatusCode.Conflict, faraConfirm.StatusCode);
            var corp = await faraConfirm.Content.ReadFromJsonAsync<JsonElement>();
            var lacune = corp.GetProperty("lacune").EnumerateArray().Select(x => x.GetInt32()).ToList();
            Assert.Equal(new[] { 1, 2, 3, 4 }, lacune);

            var cuConfirm = await admin.PostAsJsonAsync($"/api/Dispozitii/{id}/AtribuieNumar",
                new { numar = 5, confirmaCuLacune = true });
            Assert.Equal(HttpStatusCode.OK, cuConfirm.StatusCode);
            var dto = await cuConfirm.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(5, dto.GetProperty("numar").GetInt32());
        }
    }

    [Fact]
    public async Task AtribuieNumar_NumarActivLuatDeAltaDispozitie_409CuSugestie()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id1 = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id1, 1);

            var id2 = await admin.CreeazaDispozitieAsync();
            var raspuns = await admin.PostAsJsonAsync($"/api/Dispozitii/{id2}/AtribuieNumar",
                new { numar = 1, confirmaCuLacune = false });
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
            var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(2, corp.GetProperty("sugestieAlternativa").GetInt32());
        }
    }

    [Fact]
    public async Task AtribuieNumar_NumarArs_NuPoateFiRefolosit_409CuSugestie()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id1 = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id1, 1);
            await DbTest.SoftDeleteDispozitieAsync(id1); // arde nr. 1 (Delete prin API = Pas 8)

            var id2 = await admin.CreeazaDispozitieAsync();
            var raspuns = await admin.PostAsJsonAsync($"/api/Dispozitii/{id2}/AtribuieNumar",
                new { numar = 1, confirmaCuLacune = false });
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
            var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(2, corp.GetProperty("sugestieAlternativa").GetInt32());
        }
    }

    [Fact]
    public async Task SugestieNumar_UrmatorulNumarLiber_SiAnulEmiterii()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id1 = await admin.CreeazaDispozitieAsync();

            var s1 = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Dispozitii/{id1}/SugestieNumar");
            Assert.Equal(1, s1.GetProperty("numar").GetInt32());
            var anSugerat = s1.GetProperty("an").GetInt32();

            // anul sugerat coincide cu anul real de numerotare (din DataEmitere)
            await admin.AtribuieNumarDispozitieAsync(id1, 1);
            var detalii = await admin.GetFromJsonAsync<JsonElement>($"/api/Dispozitii/{id1}");
            Assert.Equal(anSugerat, detalii.GetProperty("anNumerotare").GetInt32());

            // după ce nr.1 e luat, a doua dispoziție primește sugestia 2
            var id2 = await admin.CreeazaDispozitieAsync();
            var s2 = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Dispozitii/{id2}/SugestieNumar");
            Assert.Equal(2, s2.GetProperty("numar").GetInt32());
        }
    }

    private async Task<HttpClient> AdminNouAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }
}
