using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteComunicareDispozitiePrefect
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteComunicareDispozitiePrefect(CleriqFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task Adauga_PeDispozitieNenumerotata_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync(); // rămâne Draft
            var raspuns = await admin.PostAsJsonAsync($"/api/Dispozitii/{id}/Comunicari", CorpComunicare());
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Adauga_PeDispozitieNumerotata_NumereDeOrdineSecvential()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id);

            var prima = await admin.PostAsJsonAsync($"/api/Dispozitii/{id}/Comunicari", CorpComunicare());
            Assert.Equal(HttpStatusCode.OK, prima.StatusCode);
            var dto1 = await prima.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, dto1.GetProperty("numarOrdineInRegistru").GetInt32());

            var aDoua = await admin.PostAsJsonAsync($"/api/Dispozitii/{id}/Comunicari", CorpComunicare());
            Assert.Equal(HttpStatusCode.OK, aDoua.StatusCode);
            var dto2 = await aDoua.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(2, dto2.GetProperty("numarOrdineInRegistru").GetInt32());
        }
    }

    [Fact]
    public async Task Actualizeaza_AplicaRaspuns_PastreazaImutabilele()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id);
            var creare = await admin.PostAsJsonAsync($"/api/Dispozitii/{id}/Comunicari", CorpComunicare());
            var creat = await creare.Content.ReadFromJsonAsync<JsonElement>();
            var comunicareId = creat.GetProperty("id").GetInt32();
            var canalInitial = creat.GetProperty("canalTransmitere").GetInt32();
            var ordineInitiala = creat.GetProperty("numarOrdineInRegistru").GetInt32();

            var actualizare = await admin.PutAsJsonAsync(
                $"/api/Dispozitii/{id}/Comunicari/{comunicareId}", new
                {
                    raspuns = RaspunsPrefect.Acceptat,
                    dataRaspuns = DateOnly.FromDateTime(DateTime.UtcNow),
                    obiectiiMotivate = (string?)null,
                    observatiiInterne = "verificat intern",
                    nrInregistrarePrefect = "PR-77",
                    dataConfirmarePrefect = (DateOnly?)null
                });
            Assert.Equal(HttpStatusCode.OK, actualizare.StatusCode);

            var dupa = await actualizare.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((int)RaspunsPrefect.Acceptat, dupa.GetProperty("raspunsPrefect").GetInt32());
            Assert.Equal("PR-77", dupa.GetProperty("nrInregistrarePrefect").GetString());
            // imutabile post-creare: canalul + numărul de ordine rămân neschimbate
            Assert.Equal(canalInitial, dupa.GetProperty("canalTransmitere").GetInt32());
            Assert.Equal(ordineInitiala, dupa.GetProperty("numarOrdineInRegistru").GetInt32());
        }
    }

    [Fact]
    public async Task Sterge_CaSecretar_403_CaAdmin_204()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id);
            var creare = await admin.PostAsJsonAsync($"/api/Dispozitii/{id}/Comunicari", CorpComunicare());
            var comunicareId = (await creare.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            using var secretar = await _factory.ClientSecretarAsync(admin);
            var caSecretar = await secretar.DeleteAsync($"/api/Dispozitii/{id}/Comunicari/{comunicareId}");
            Assert.Equal(HttpStatusCode.Forbidden, caSecretar.StatusCode);

            var caAdmin = await admin.DeleteAsync($"/api/Dispozitii/{id}/Comunicari/{comunicareId}");
            Assert.Equal(HttpStatusCode.NoContent, caAdmin.StatusCode);
        }
    }

    // Comunicarea la prefect îngheață singură varianta semnată (latch), fără publicare MOL.
    [Fact]
    public async Task Adauga_IngheataVariantaSemnata_409LaReplaceSiDelete()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id);
            await admin.SemneazaDispozitieAsync(id);

            var prima = await admin.IncarcaDispozitieSemnatAsync(
                id, Encoding.UTF8.GetBytes("%PDF-1.4 v1"), "semnat.pdf");
            Assert.Equal(HttpStatusCode.OK, prima.StatusCode);

            await admin.AdaugaComunicarePrefectDispozitieAsync(id);

            var replace = await admin.IncarcaDispozitieSemnatAsync(
                id, Encoding.UTF8.GetBytes("%PDF-1.4 v2"), "semnat-2.pdf");
            Assert.Equal(HttpStatusCode.Conflict, replace.StatusCode);

            var stergere = await admin.DeleteAsync($"/api/Dispozitii/{id}/Semnat");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    // Garda #1 din matricea DELETE a dispoziției: registrul prefectului e inviolabil.
    [Fact]
    public async Task StergeDispozitie_CuComunicariActive_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id);
            await admin.AdaugaComunicarePrefectDispozitieAsync(id);

            var stergere = await admin.DeleteAsync($"/api/Dispozitii/{id}");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    // „Anulează MOL" blocată după comunicarea la prefect (punct de neîntoarcere, art. 197).
    [Fact]
    public async Task AnulareMol_DupaComunicareLaPrefect_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync(TipDispozitie.Normativ);
            await admin.AtribuieNumarDispozitieAsync(id);
            await admin.SemneazaDispozitieAsync(id);
            await admin.PublicaMolDispozitieAsync(id);
            await admin.AdaugaComunicarePrefectDispozitieAsync(id);

            var anulare = await admin.AnuleazaMolDispozitieAsync(id, "Vreau să anulez publicarea");
            Assert.Equal(HttpStatusCode.Conflict, anulare.StatusCode);
        }
    }

    // === T-3 dashboard + Detalii include (10b) ===

    [Fact]
    public async Task UrgentDeComunicat_IncludeNecomunicata_ExcludeComunicataSiDraft()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            // numerotată, necomunicată → apare
            var necomunicata = await admin.CreeazaDispozitieAsync(TipDispozitie.Normativ);
            await admin.AtribuieNumarDispozitieAsync(necomunicata, 1);

            // numerotată + comunicată → NU apare (are comunicare live în registru)
            var comunicata = await admin.CreeazaDispozitieAsync(TipDispozitie.Normativ);
            await admin.AtribuieNumarDispozitieAsync(comunicata, 2);
            await admin.AdaugaComunicarePrefectDispozitieAsync(comunicata);

            // draft (nenumerotată) → NU apare (Status < Numerotat)
            var draft = await admin.CreeazaDispozitieAsync(TipDispozitie.Normativ);

            // prag mare ca să prindem termenul de ~10 zile lucrătoare al unei dispoziții emise azi
            var lista = await admin.GetFromJsonAsync<JsonElement>("/api/Dispozitii/UrgentDeComunicat?prag=30");
            var ids = lista.EnumerateArray().Select(x => x.GetProperty("dispozitieId").GetInt32()).ToList();

            Assert.Contains(necomunicata, ids);
            Assert.DoesNotContain(comunicata, ids);
            Assert.DoesNotContain(draft, ids);
        }
    }

    [Fact]
    public async Task Detalii_IncludeComunicarile()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id);
            await admin.AdaugaComunicarePrefectDispozitieAsync(id);

            var detalii = await admin.GetFromJsonAsync<JsonElement>($"/api/Dispozitii/{id}");
            var comunicari = detalii.GetProperty("comunicari");
            Assert.Equal(1, comunicari.GetArrayLength());
            Assert.Equal(id, comunicari[0].GetProperty("dispozitieId").GetInt32());
            Assert.Equal(1, comunicari[0].GetProperty("numarOrdineInRegistru").GetInt32());
        }
    }

    private static object CorpComunicare() => new
    {
        dataTrimiteri = DateOnly.FromDateTime(DateTime.UtcNow),
        canalTransmitere = CanalTransmiterePrefect.EmailOficial,
        nrInregistrarePrefect = (string?)null,
        dataConfirmarePrefect = (DateOnly?)null,
        observatiiInterne = (string?)null
    };

    private async Task<HttpClient> AdminNouAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }
}
