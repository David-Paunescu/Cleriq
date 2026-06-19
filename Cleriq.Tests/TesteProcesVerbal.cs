using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteProcesVerbal
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteProcesVerbal(CleriqFixture fixture) => _factory = fixture.Factory;

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
    public async Task Genereaza_CreeazaDraft_CuPrezentaCvorumSiVoturiNominale()
    {
        var (admin, sedintaId, c) = await ScenariuAsync(2);
        using (admin)
        {
            await admin.SeteazaPrezentaAsync(sedintaId, c[0], StatusPrezenta.Prezent);
            await admin.SeteazaPrezentaAsync(sedintaId, c[1], StatusPrezenta.Prezent);
            var punctId = await admin.CreeazaPunctAsync(sedintaId, TipMajoritate.Simpla);
            await admin.VoteazaAsync(sedintaId, punctId, c[0], OptiuneVot.Pentru);
            await admin.VoteazaAsync(sedintaId, punctId, c[1], OptiuneVot.Pentru);
            await admin.InchideVotAsync(sedintaId, punctId);

            var pv = await admin.GenereazaProcesVerbalAsync(sedintaId);

            Assert.Equal((int)StatusProcesVerbal.Draft, pv.GetProperty("status").GetInt32());
            Assert.NotEqual(JsonValueKind.Null, pv.GetProperty("dataGenerare").ValueKind);

            var continut = pv.GetProperty("continut").GetString()!;
            Assert.Contains("- Consilier 01: Prezent", continut);
            Assert.Contains("**Cvorum:** întrunit", continut);
            Assert.Contains("**Rezultat:** Adoptat", continut);
            Assert.Contains("Pentru (2): Consilier 01, Consilier 02", continut);
        }
    }

    [Fact]
    public async Task Genereaza_VotSecret_TallyVizibilFaraNominale()
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

            var pv = await admin.GenereazaProcesVerbalAsync(sedintaId);
            var continut = pv.GetProperty("continut").GetString()!;

            Assert.Contains("Vot secret", continut);
            Assert.Contains("- Pentru: 1", continut);
            Assert.Contains("- Împotrivă: 1", continut);
            Assert.DoesNotContain("Voturi nominale", continut);
        }
    }

    [Fact]
    public async Task Regenerare_SuprascrieEditarileManuale()
    {
        var (admin, sedintaId, _) = await ScenariuAsync(1);
        using (admin)
        {
            await admin.GenereazaProcesVerbalAsync(sedintaId);

            const string textManual = "TEXT EDITAT MANUAL DE SECRETAR";
            var editare = await admin.PutAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/ProcesVerbal", new { continut = textManual });
            Assert.Equal(HttpStatusCode.OK, editare.StatusCode);

            var dupaEditare = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/ProcesVerbal");
            Assert.Contains(textManual, dupaEditare.GetProperty("continut").GetString());

            var regenerat = await admin.GenereazaProcesVerbalAsync(sedintaId);
            var continut = regenerat.GetProperty("continut").GetString()!;
            Assert.DoesNotContain(textManual, continut);
            Assert.Contains("# Proces verbal", continut);
        }
    }

    [Fact]
    public async Task Finalizare_BlocheazaEditareaRegenerareaSiRefinalizarea()
    {
        var (admin, sedintaId, _) = await ScenariuAsync(1);
        using (admin)
        {
            await admin.GenereazaProcesVerbalAsync(sedintaId);
            await admin.FinalizeazaProcesVerbalAsync(sedintaId);

            var pv = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/ProcesVerbal");
            Assert.Equal((int)StatusProcesVerbal.Finalizat, pv.GetProperty("status").GetInt32());
            Assert.NotEqual(JsonValueKind.Null, pv.GetProperty("dataFinalizare").ValueKind);

            var editare = await admin.PutAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/ProcesVerbal", new { continut = "x" });
            Assert.Equal(HttpStatusCode.Conflict, editare.StatusCode);

            var regenerare = await admin.PostAsync(
                $"/api/Sedinte/{sedintaId}/ProcesVerbal/Genereaza", null);
            Assert.Equal(HttpStatusCode.Conflict, regenerare.StatusCode);

            var refinalizare = await admin.PostAsync(
                $"/api/Sedinte/{sedintaId}/ProcesVerbal/Finalizeaza", null);
            Assert.Equal(HttpStatusCode.Conflict, refinalizare.StatusCode);
        }
    }

    [Fact]
    public async Task Pdf_PeDraft_ReturneazaPdfValid()
    {
        var (admin, sedintaId, _) = await ScenariuAsync(1);
        using (admin)
        {
            await admin.GenereazaProcesVerbalAsync(sedintaId);

            var raspuns = await admin.GetAsync($"/api/Sedinte/{sedintaId}/ProcesVerbal/Pdf");

            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
            Assert.Equal("application/pdf", raspuns.Content.Headers.ContentType!.MediaType);
            var bytes = await raspuns.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.Length > 100);
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        }
    }

    [Fact]
    public async Task Semnat_PePvDraft_Returneaza409()
    {
        var (admin, sedintaId, _) = await ScenariuAsync(1);
        using (admin)
        {
            await admin.GenereazaProcesVerbalAsync(sedintaId);

            var raspuns = await admin.IncarcaPvSemnatAsync(
                sedintaId, Encoding.UTF8.GetBytes("%PDF-1.4 fals"), "pv-semnat.pdf");

            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Semnat_AltaExtensieDecatPdf_Returneaza400()
    {
        var (admin, sedintaId, _) = await ScenariuAsync(1);
        using (admin)
        {
            await admin.GenereazaProcesVerbalAsync(sedintaId);
            await admin.FinalizeazaProcesVerbalAsync(sedintaId);

            var raspuns = await admin.IncarcaPvSemnatAsync(
                sedintaId, Encoding.UTF8.GetBytes("nu e pdf"), "pv-semnat.docx");

            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Semnat_UploadDescarcareReplaceSiStergere()
    {
        var (admin, sedintaId, _) = await ScenariuAsync(1);
        using (admin)
        {
            await admin.GenereazaProcesVerbalAsync(sedintaId);
            await admin.FinalizeazaProcesVerbalAsync(sedintaId);

            var bytesV1 = Encoding.UTF8.GetBytes("%PDF-1.4 varianta semnata unu");
            var upload = await admin.IncarcaPvSemnatAsync(sedintaId, bytesV1, "pv-semnat-v1.pdf");
            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

            var dto = await upload.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("pv-semnat-v1.pdf", dto.GetProperty("numeFisierSemnat").GetString());
            Assert.Equal((long)bytesV1.Length, dto.GetProperty("marimeSemnat").GetInt64());
            Assert.NotEqual(JsonValueKind.Null, dto.GetProperty("dataIncarcareSemnat").ValueKind);

            var descarcare = await admin.GetAsync($"/api/Sedinte/{sedintaId}/ProcesVerbal/Semnat");
            Assert.Equal(HttpStatusCode.OK, descarcare.StatusCode);
            Assert.Equal("application/pdf", descarcare.Content.Headers.ContentType!.MediaType);
            Assert.Equal(bytesV1, await descarcare.Content.ReadAsByteArrayAsync());

            var bytesV2 = Encoding.UTF8.GetBytes("%PDF-1.4 varianta semnata doi, mai lunga");
            var replace = await admin.IncarcaPvSemnatAsync(sedintaId, bytesV2, "pv-semnat-v2.pdf");
            Assert.Equal(HttpStatusCode.OK, replace.StatusCode);

            var dupaReplace = await admin.GetAsync($"/api/Sedinte/{sedintaId}/ProcesVerbal/Semnat");
            Assert.Equal(bytesV2, await dupaReplace.Content.ReadAsByteArrayAsync());

            var stergere = await admin.DeleteAsync($"/api/Sedinte/{sedintaId}/ProcesVerbal/Semnat");
            Assert.Equal(HttpStatusCode.NoContent, stergere.StatusCode);

            var dupaStergere = await admin.GetAsync($"/api/Sedinte/{sedintaId}/ProcesVerbal/Semnat");
            Assert.Equal(HttpStatusCode.NotFound, dupaStergere.StatusCode);

            var pv = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{sedintaId}/ProcesVerbal");
            Assert.Equal(JsonValueKind.Null, pv.GetProperty("numeFisierSemnat").ValueKind);
        }
    }

    [Fact]
    public async Task OperatiiPeSedintaFaraPv_Returneaza404()
    {
        var (admin, sedintaId, _) = await ScenariuAsync(1);
        using (admin)
        {
            var obtinere = await admin.GetAsync($"/api/Sedinte/{sedintaId}/ProcesVerbal");
            Assert.Equal(HttpStatusCode.NotFound, obtinere.StatusCode);

            var editare = await admin.PutAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/ProcesVerbal", new { continut = "x" });
            Assert.Equal(HttpStatusCode.NotFound, editare.StatusCode);

            var finalizare = await admin.PostAsync(
                $"/api/Sedinte/{sedintaId}/ProcesVerbal/Finalizeaza", null);
            Assert.Equal(HttpStatusCode.NotFound, finalizare.StatusCode);

            var semnat = await admin.IncarcaPvSemnatAsync(
                sedintaId, Encoding.UTF8.GetBytes("%PDF"), "x.pdf");
            Assert.Equal(HttpStatusCode.NotFound, semnat.StatusCode);
        }
    }

    // === Aproba ===

    [Fact]
    public async Task Aproba_SedintaFinalizata_200SiCampurileSetate()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.Finalizata);
            var raspuns = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });

            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
            var pv = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotEqual(JsonValueKind.Null, pv.GetProperty("dataAprobare").ValueKind);
            Assert.Equal(sedintaAprobareId, pv.GetProperty("aprobatInSedintaId").GetInt32());
            Assert.False(string.IsNullOrEmpty(pv.GetProperty("aprobatInSedintaTitlu").GetString()));
            Assert.NotEqual(JsonValueKind.Null, pv.GetProperty("aprobatInSedintaDataOra").ValueKind);
        }
    }

    [Fact]
    public async Task Aproba_SedintaConvocata_200()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.Convocata);
            var raspuns = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });
            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Aproba_SedintaInDesfasurare_200()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.InDesfasurare);
            var raspuns = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });
            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Aproba_PvDraft_409()
    {
        var (admin, sedintaPvId, _) = await ScenariuAsync(1);
        using (admin)
        {
            await admin.GenereazaProcesVerbalAsync(sedintaPvId);
            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.Finalizata);
            var raspuns = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Aproba_PvDejaAprobat_409()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.Finalizata);
            var prima = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });
            Assert.Equal(HttpStatusCode.OK, prima.StatusCode);

            var aDoua = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });
            Assert.Equal(HttpStatusCode.Conflict, aDoua.StatusCode);
        }
    }

    [Fact]
    public async Task Aproba_AceeasiSedinta_409()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            var raspuns = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaPvId });
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Aproba_SedintaAltTenant_404()
    {
        var (admin1, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin1)
        {
            var inst2 = await _factory.ProvisioneazaInstitutieAsync();
            using var admin2 = await _factory.ClientAutentificatAsync(inst2.EmailAdmin, inst2.ParolaAdmin);
            var sedintaAltTenant = await CreeazaSedintaInStatusAsync(admin2, StatusSedinta.Finalizata);

            var raspuns = await admin1.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAltTenant });
            Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Aproba_SedintaPlanificata_409()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.Planificata);
            var raspuns = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Aproba_SedintaAnulata_409()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.Anulata);
            var raspuns = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Aproba_CaSecretar_200()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            using var secretar = await CreeazaSecretarAsync(admin);
            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.Finalizata);
            var raspuns = await secretar.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });
            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Aproba_CaConsilier_403()
    {
        var (admin, sedintaPvId, consilieri) = await ScenariuAsync(1);
        using (admin)
        {
            await admin.GenereazaProcesVerbalAsync(sedintaPvId);
            await admin.FinalizeazaProcesVerbalAsync(sedintaPvId);
            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.Finalizata);

            using var consilier = await _factory.ClientConsilierAsync(admin, consilieri[0]);
            var raspuns = await consilier.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });
            Assert.Equal(HttpStatusCode.Forbidden, raspuns.StatusCode);
        }
    }

    // === Dezaproba ===

    [Fact]
    public async Task Dezaproba_CaAdmin_200()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.Finalizata);
            await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });

            var dezaprobare = await admin.DeleteAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba");
            Assert.Equal(HttpStatusCode.OK, dezaprobare.StatusCode);

            var pv = await dezaprobare.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Null, pv.GetProperty("dataAprobare").ValueKind);
            Assert.Equal(JsonValueKind.Null, pv.GetProperty("aprobatInSedintaId").ValueKind);
            Assert.Equal(JsonValueKind.Null, pv.GetProperty("aprobatInSedintaTitlu").ValueKind);
            Assert.Equal(JsonValueKind.Null, pv.GetProperty("aprobatInSedintaDataOra").ValueKind);
        }
    }

    [Fact]
    public async Task Dezaproba_PvNeaprobat_409()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            var raspuns = await admin.DeleteAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba");
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Dezaproba_CaSecretar_403()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.Finalizata);
            await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });

            using var secretar = await CreeazaSecretarAsync(admin);
            var raspuns = await secretar.DeleteAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba");
            Assert.Equal(HttpStatusCode.Forbidden, raspuns.StatusCode);
        }
    }

    // === Gărzi pe acțiuni distructive după aprobare ===

    [Fact]
    public async Task IncarcaSemnat_DupaAprobare_409()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.Finalizata);
            await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });

            var raspuns = await admin.IncarcaPvSemnatAsync(
                sedintaPvId, Encoding.UTF8.GetBytes("%PDF-1.4 semnat"), "pv-semnat.pdf");
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task StergeSemnat_DupaAprobare_409()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            var upload = await admin.IncarcaPvSemnatAsync(
                sedintaPvId, Encoding.UTF8.GetBytes("%PDF-1.4 semnat"), "pv-semnat.pdf");
            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.Finalizata);
            await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });

            var stergere = await admin.DeleteAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Semnat");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    [Fact]
    public async Task StergeSedinta_CareAprobaPv_409()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.Convocata);
            await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });

            var stergere = await admin.DeleteAsync($"/api/Sedinte/{sedintaAprobareId}");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    [Fact]
    public async Task StergeSedinta_AlCareiPvEsteAprobat_409()
    {
        var (admin, sedintaPvId) = await ScenariuPvFinalizatAsync();
        using (admin)
        {
            var sedintaAprobareId = await CreeazaSedintaInStatusAsync(admin, StatusSedinta.Finalizata);
            await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaPvId}/ProcesVerbal/Aproba",
                new { aprobatInSedintaId = sedintaAprobareId });

            var stergere = await admin.DeleteAsync($"/api/Sedinte/{sedintaPvId}");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    // === Helpere private pentru aprobare PV ===

    private async Task<(HttpClient Admin, int SedintaId)> ScenariuPvFinalizatAsync()
    {
        var (admin, sedintaId, _) = await ScenariuAsync(1);
        await admin.GenereazaProcesVerbalAsync(sedintaId);
        await admin.FinalizeazaProcesVerbalAsync(sedintaId);
        return (admin, sedintaId);
    }

    private async Task<int> CreeazaSedintaInStatusAsync(HttpClient admin, StatusSedinta status)
    {
        var sedintaId = await admin.CreeazaSedintaAsync();
        if (status != StatusSedinta.Planificata)
            await DbTest.SeteazaStatusSedintaAsync(sedintaId, status);
        return sedintaId;
    }

    private async Task<HttpClient> CreeazaSecretarAsync(HttpClient admin)
    {
        var email = $"secretar-{Guid.NewGuid():N}@test.ro";
        const string parola = "Secretar1!";
        var register = await admin.PostAsJsonAsync("/api/Auth/register", new
        {
            email,
            parola,
            numeComplet = "Secretar Test",
            rol = "Secretar"
        });
        Assert.True(register.IsSuccessStatusCode, await register.Content.ReadAsStringAsync());
        return await _factory.ClientAutentificatAsync(email, parola);
    }
}