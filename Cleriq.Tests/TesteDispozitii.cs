using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteDispozitii
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteDispozitii(CleriqFixture fixture) => _factory = fixture.Factory;

    // === Creare + derivare semnatari + conținut ===

    [Fact]
    public async Task Creeaza_DeriveazaEmitentSiSecretar_CreeazaDraftCuContinut()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync(TipDispozitie.Normativ, "delegarea de atribuții");

            var detalii = await admin.GetFromJsonAsync<JsonElement>($"/api/Dispozitii/{id}");
            Assert.Equal((int)StatusActRedactional.Draft, detalii.GetProperty("status").GetInt32());
            Assert.Equal("delegarea de atribuții", detalii.GetProperty("titlu").GetString());
            Assert.Equal((int)TipDispozitie.Normativ, detalii.GetProperty("tipDispozitie").GetInt32());
            Assert.Equal(JsonValueKind.Null, detalii.GetProperty("numar").ValueKind);

            // Conținutul reflectă actul unilateral al primarului, fără vot/cvorum
            var continut = detalii.GetProperty("continut").GetString()!;
            Assert.Contains("DISPOZIȚIA Nr.", continut);
            Assert.Contains("_[urmează să fie atribuit]_", continut); // placeholder pe Draft
            Assert.Contains("PRIMARUL", continut);
            Assert.Contains("DISPUNE:", continut);
            Assert.Contains("art. 196 alin. (1) lit. b)", continut);
            Assert.Contains("SECRETAR GENERAL", continut);
            Assert.DoesNotContain("voturi", continut);
            Assert.DoesNotContain("HOTĂRĂȘTE", continut);

            // Doi semnatari: Emitent (Persoana primar) + SecretarContrasemnatura (Persoana secretar)
            var semnatari = detalii.GetProperty("semnatari").EnumerateArray().ToList();
            Assert.Equal(2, semnatari.Count);
            Assert.Contains(semnatari, s =>
                s.GetProperty("rolSemnatar").GetInt32() == (int)RolSemnatarDispozitie.Emitent
                && s.GetProperty("persoanaId").ValueKind != JsonValueKind.Null
                && s.GetProperty("consilierId").ValueKind == JsonValueKind.Null);
            Assert.Contains(semnatari, s =>
                s.GetProperty("rolSemnatar").GetInt32() == (int)RolSemnatarDispozitie.SecretarContrasemnatura
                && s.GetProperty("persoanaId").ValueKind != JsonValueKind.Null);
        }
    }

    [Fact]
    public async Task Creeaza_FaraPrimar_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            // secretar prezent, dar niciun mandat de primar și fără override → precondiția cade
            var raspuns = await admin.CreeazaDispozitieRaspunsAsync(asigurMandatPrimar: false);
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Creeaza_FaraSecretar_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            // primar prezent, dar niciun mandat de secretar UAT → 400 conștient (substitut amânat)
            var raspuns = await admin.CreeazaDispozitieRaspunsAsync(asigurMandatSecretar: false);
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Creeaza_FaraTitlu_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var raspuns = await admin.CreeazaDispozitieRaspunsAsync(titlu: "   ");
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    // === Override emitent — viceprimar înlocuitor de drept (art. 163), fără primar titular ===

    [Fact]
    public async Task Creeaza_CuEmitentConsilierOverride_FolosesteInlocuitorPPrimar()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var consilierId = await admin.CreeazaConsilierAsync("Ionel Înlocuitor");

            // niciun mandat de primar pe tenant; emitentul vine ca override de consilier (viceprimar)
            var id = await admin.CreeazaDispozitieAsync(emitentConsilierId: consilierId);

            var detalii = await admin.GetFromJsonAsync<JsonElement>($"/api/Dispozitii/{id}");
            var semnatari = detalii.GetProperty("semnatari").EnumerateArray().ToList();
            var emitent = semnatari.Single(s =>
                s.GetProperty("rolSemnatar").GetInt32() == (int)RolSemnatarDispozitie.Emitent);
            Assert.Equal(consilierId, emitent.GetProperty("consilierId").GetInt32());
            Assert.Equal(JsonValueKind.Null, emitent.GetProperty("persoanaId").ValueKind);

            var continut = detalii.GetProperty("continut").GetString()!;
            Assert.Contains("p. PRIMAR", continut);
            Assert.Contains("Viceprimar", continut);
            Assert.Contains("Ionel Înlocuitor", continut);
        }
    }

    [Fact]
    public async Task Creeaza_CuEmitentConsilierInexistent_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var raspuns = await admin.CreeazaDispozitieRaspunsAsync(emitentConsilierId: 999999);
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    // === Conținut: editabil pe Draft, blocat pe Semnat ===

    [Fact]
    public async Task Continut_EditabilPeDraft_BlocatDupaSemnare()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();

            var editare = await admin.PutAsJsonAsync($"/api/Dispozitii/{id}/Continut",
                new { continut = "## DISPUNE:\n\nArt. 1. Text editat de secretar." });
            Assert.Equal(HttpStatusCode.OK, editare.StatusCode);
            var dtoEdit = await editare.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains("Text editat de secretar", dtoEdit.GetProperty("continut").GetString()!);

            // Regenerare → revine la template-ul generat
            var regen = await admin.PostAsync($"/api/Dispozitii/{id}/RegenereazaContinut", null);
            Assert.Equal(HttpStatusCode.OK, regen.StatusCode);
            var dtoRegen = await regen.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains("DISPOZIȚIA Nr.", dtoRegen.GetProperty("continut").GetString()!);

            // Semnarea e Pas 6; forțăm statusul ca să verificăm gărzile de pe Semnat
            await DbTest.SeteazaStatusDispozitieAsync(id, StatusActRedactional.Semnat);

            var editareDupaSemnare = await admin.PutAsJsonAsync(
                $"/api/Dispozitii/{id}/Continut", new { continut = "alt text" });
            Assert.Equal(HttpStatusCode.Conflict, editareDupaSemnare.StatusCode);

            var regenerareDupaSemnare = await admin.PostAsync(
                $"/api/Dispozitii/{id}/RegenereazaContinut", null);
            Assert.Equal(HttpStatusCode.Conflict, regenerareDupaSemnare.StatusCode);
        }
    }

    // === Semnare + contrasemnătură refuzată (Pas 6) ===

    [Fact]
    public async Task Semneaza_BlocatPeDraft_PermisPeNumerotat()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();

            var peDraft = await admin.PostAsync($"/api/Dispozitii/{id}/Semneaza", null);
            Assert.Equal(HttpStatusCode.Conflict, peDraft.StatusCode);

            await admin.AtribuieNumarDispozitieAsync(id);

            var peNumerotat = await admin.PostAsync($"/api/Dispozitii/{id}/Semneaza", null);
            Assert.Equal(HttpStatusCode.OK, peNumerotat.StatusCode);
            var dto = await peNumerotat.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((int)StatusActRedactional.Semnat, dto.GetProperty("status").GetInt32());
        }
    }

    [Fact]
    public async Task RefuzContrasemnare_SoftStergeSecretar_SiPermiteSemnareaPesteRefuz()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id);

            var refuz = await admin.RefuzContrasemnareDispozitieAsync(id, "Proiectul încalcă art. 5.");
            Assert.Equal(HttpStatusCode.OK, refuz.StatusCode);
            var dtoRefuz = await refuz.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(dtoRefuz.GetProperty("contrasemnaturaRefuzata").GetBoolean());
            Assert.Equal("Proiectul încalcă art. 5.",
                dtoRefuz.GetProperty("obiectieLegalitateSecretar").GetString());
            Assert.NotEqual(JsonValueKind.Null, dtoRefuz.GetProperty("dataRefuzContrasemnare").ValueKind);

            // rândul de secretar a fost soft-șters → rămâne doar emitentul
            var semnatari = dtoRefuz.GetProperty("semnatari").EnumerateArray().ToList();
            Assert.Single(semnatari);
            Assert.Equal((int)RolSemnatarDispozitie.Emitent,
                semnatari[0].GetProperty("rolSemnatar").GetInt32());

            // semnarea trece pe ramura „SAU refuz motivat"
            var semnare = await admin.PostAsync($"/api/Dispozitii/{id}/Semneaza", null);
            Assert.Equal(HttpStatusCode.OK, semnare.StatusCode);
            var dtoSemn = await semnare.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((int)StatusActRedactional.Semnat, dtoSemn.GetProperty("status").GetInt32());
        }
    }

    [Fact]
    public async Task RefuzContrasemnare_FaraObiectie_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            var refuz = await admin.RefuzContrasemnareDispozitieAsync(id, "   ");
            Assert.Equal(HttpStatusCode.BadRequest, refuz.StatusCode);
        }
    }

    [Fact]
    public async Task RefuzContrasemnare_Dublu_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            var r1 = await admin.RefuzContrasemnareDispozitieAsync(id);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            var r2 = await admin.RefuzContrasemnareDispozitieAsync(id);
            Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
        }
    }

    [Fact]
    public async Task RefuzContrasemnare_PeDispozitieSemnata_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id);
            await admin.SemneazaDispozitieAsync(id); // contrasemnătură normală

            var refuz = await admin.RefuzContrasemnareDispozitieAsync(id);
            Assert.Equal(HttpStatusCode.Conflict, refuz.StatusCode);
        }
    }

    [Fact]
    public async Task RefuzContrasemnare_ApoiRegenereaza_ContinutArataRefuzul()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            var refuz = await admin.RefuzContrasemnareDispozitieAsync(id, "Lipsește avizul financiar.");
            Assert.Equal(HttpStatusCode.OK, refuz.StatusCode);

            var regen = await admin.PostAsync($"/api/Dispozitii/{id}/RegenereazaContinut", null);
            Assert.Equal(HttpStatusCode.OK, regen.StatusCode);
            var continut = (await regen.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("continut").GetString()!;
            Assert.Contains("REFUZATĂ", continut);
            Assert.Contains("Lipsește avizul financiar.", continut);
            Assert.DoesNotContain("Contrasemnează pentru legalitate", continut);
        }
    }

    // === Variantă semnată (Pas 7) ===

    [Fact]
    public async Task Semnat_UploadDescarcareReplaceStergere()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id);
            await admin.SemneazaDispozitieAsync(id);

            var bytesV1 = Encoding.UTF8.GetBytes("%PDF-1.4 dispozitie semnata unu");
            var upload = await admin.IncarcaDispozitieSemnatAsync(id, bytesV1, "disp-semnat-v1.pdf");
            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
            var dto = await upload.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(dto.GetProperty("esteSemnat").GetBoolean());
            Assert.Equal("disp-semnat-v1.pdf", dto.GetProperty("numeFisierSemnat").GetString());
            Assert.Equal((long)bytesV1.Length, dto.GetProperty("marimeSemnat").GetInt64());

            var descarcare = await admin.GetAsync($"/api/Dispozitii/{id}/Semnat");
            Assert.Equal(HttpStatusCode.OK, descarcare.StatusCode);
            Assert.Equal("application/pdf", descarcare.Content.Headers.ContentType!.MediaType);
            Assert.Equal(bytesV1, await descarcare.Content.ReadAsByteArrayAsync());

            var bytesV2 = Encoding.UTF8.GetBytes("%PDF-1.4 dispozitie semnata doi, mai lung");
            var replace = await admin.IncarcaDispozitieSemnatAsync(id, bytesV2, "disp-semnat-v2.pdf");
            Assert.Equal(HttpStatusCode.OK, replace.StatusCode);
            var dupaReplace = await admin.GetAsync($"/api/Dispozitii/{id}/Semnat");
            Assert.Equal(bytesV2, await dupaReplace.Content.ReadAsByteArrayAsync());

            var stergere = await admin.DeleteAsync($"/api/Dispozitii/{id}/Semnat");
            Assert.Equal(HttpStatusCode.NoContent, stergere.StatusCode);

            var dupaStergere = await admin.GetAsync($"/api/Dispozitii/{id}/Semnat");
            Assert.Equal(HttpStatusCode.NotFound, dupaStergere.StatusCode);
        }
    }

    [Fact]
    public async Task Semnat_PeDispozitieNesemnata_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync(); // rămâne Draft
            var raspuns = await admin.IncarcaDispozitieSemnatAsync(
                id, Encoding.UTF8.GetBytes("%PDF-1.4"), "x.pdf");
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Semnat_DupaCircuit_PrimaAtasareOk_ReplaceSiStergere_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id);
            await admin.SemneazaDispozitieAsync(id);
            await DbTest.SeteazaAIntratInCircuitDispozitieAsync(id);

            // prima atașare post-circuit e permisă (upgrade benign — paritar HCL varianta B)
            var prima = await admin.IncarcaDispozitieSemnatAsync(
                id, Encoding.UTF8.GetBytes("%PDF-1.4 prima"), "disp-semnat.pdf");
            Assert.Equal(HttpStatusCode.OK, prima.StatusCode);

            // replace după intrarea în circuit → 409 (dovada e intangibilă)
            var replace = await admin.IncarcaDispozitieSemnatAsync(
                id, Encoding.UTF8.GetBytes("%PDF-1.4 replace"), "disp-semnat-2.pdf");
            Assert.Equal(HttpStatusCode.Conflict, replace.StatusCode);

            // DELETE după intrarea în circuit → 409
            var stergere = await admin.DeleteAsync($"/api/Dispozitii/{id}/Semnat");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    // === Invalidare + revocare (Pas 8) ===

    [Fact]
    public async Task Invalidare_Altul_FaraText_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            var resp = await admin.InvalideazaDispozitieAsync(id, MotivInvalidare.Altul);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }
    }

    [Fact]
    public async Task Invalidare_MotivNecunoscut_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            var resp = await admin.InvalideazaDispozitieAsync(id, (MotivInvalidare)999);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }
    }

    [Fact]
    public async Task Invalidare_Dubla_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            var prima = await admin.InvalideazaDispozitieAsync(id, MotivInvalidare.Caduc);
            Assert.Equal(HttpStatusCode.OK, prima.StatusCode);
            var doua = await admin.InvalideazaDispozitieAsync(id, MotivInvalidare.Caduc);
            Assert.Equal(HttpStatusCode.Conflict, doua.StatusCode);
        }
    }

    [Fact]
    public async Task Invalidare_Retractat_Normativ_OK_CuEtichetaDedicata()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync(TipDispozitie.Normativ);
            var resp = await admin.InvalideazaDispozitieAsync(id, MotivInvalidare.Retractat);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var dto = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.NotEqual(JsonValueKind.Null, dto.GetProperty("dataInvalidare").ValueKind);
            // eticheta act-aware (nu „Retractat" generic, nu „hotărâre")
            Assert.Equal("Revocat de primar (emitent)",
                dto.GetProperty("motivInvalidareEticheta").GetString());
        }
    }

    [Fact]
    public async Task Invalidare_Retractat_IndividualNeintratInCircuit_OK()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync(TipDispozitie.Individual);
            // încă nu a intrat în circuit → revocarea proprie e admisă
            var resp = await admin.InvalideazaDispozitieAsync(id, MotivInvalidare.Retractat);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }
    }

    [Fact]
    public async Task Invalidare_Retractat_IndividualInCircuit_409_DarAnulatInstantaOK()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync(TipDispozitie.Individual);
            await DbTest.SeteazaAIntratInCircuitDispozitieAsync(id);

            // individuală intrată în circuit → revocarea proprie e blocată
            var retractat = await admin.InvalideazaDispozitieAsync(id, MotivInvalidare.Retractat);
            Assert.Equal(HttpStatusCode.Conflict, retractat.StatusCode);

            // dar anularea de instanță rămâne posibilă (regula e specifică doar revocării proprii)
            var anulat = await admin.InvalideazaDispozitieAsync(
                id, MotivInvalidare.AnulatInstanta, refInvalidare: "Sent. civ. 10/2026");
            Assert.Equal(HttpStatusCode.OK, anulat.StatusCode);
        }
    }

    [Fact]
    public async Task AnuleazaInvalidare_RevineLaValid()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.InvalideazaDispozitieAsync(id, MotivInvalidare.Caduc);

            var anulare = await admin.DeleteAsync($"/api/Dispozitii/{id}/Invalidare");
            Assert.Equal(HttpStatusCode.OK, anulare.StatusCode);
            var dto = await anulare.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Null, dto.GetProperty("dataInvalidare").ValueKind);
        }
    }

    // === Ștergere (matricea de gărzi, Pas 8) ===

    [Fact]
    public async Task Sterge_DraftFaraDependinte_204()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            var stergere = await admin.DeleteAsync($"/api/Dispozitii/{id}");
            Assert.Equal(HttpStatusCode.NoContent, stergere.StatusCode);

            var dupa = await admin.GetAsync($"/api/Dispozitii/{id}");
            Assert.Equal(HttpStatusCode.NotFound, dupa.StatusCode);
        }
    }

    [Fact]
    public async Task Sterge_Semnat_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id);
            await admin.SemneazaDispozitieAsync(id);

            var stergere = await admin.DeleteAsync($"/api/Dispozitii/{id}");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    [Fact]
    public async Task Sterge_Publicat_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id);
            await DbTest.SeteazaEstePublicatDispozitieAsync(id);

            var stergere = await admin.DeleteAsync($"/api/Dispozitii/{id}");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    [Fact]
    public async Task Sterge_Invalidat_204_OverrideChiarSiPeSemnat()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var id = await admin.CreeazaDispozitieAsync();
            await admin.AtribuieNumarDispozitieAsync(id);
            await admin.SemneazaDispozitieAsync(id);

            var invalidare = await admin.InvalideazaDispozitieAsync(
                id, MotivInvalidare.AnulatInstanta, refInvalidare: "Sent. civ. 5/2026");
            Assert.Equal(HttpStatusCode.OK, invalidare.StatusCode);

            // invalidat → garda override permite ștergerea chiar și a unui act semnat
            var stergere = await admin.DeleteAsync($"/api/Dispozitii/{id}");
            Assert.Equal(HttpStatusCode.NoContent, stergere.StatusCode);
        }
    }

    // === Listă ===

    [Fact]
    public async Task Lista_IntoarceDispozitiileTenantului_FiltrabilPeTip()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var idNormativ = await admin.CreeazaDispozitieAsync(TipDispozitie.Normativ, "act normativ");
            var idIndividual = await admin.CreeazaDispozitieAsync(TipDispozitie.Individual, "act individual");

            var toate = await admin.GetFromJsonAsync<JsonElement>("/api/Dispozitii");
            var ids = toate.EnumerateArray().Select(d => d.GetProperty("id").GetInt32()).ToList();
            Assert.Contains(idNormativ, ids);
            Assert.Contains(idIndividual, ids);

            var doarIndividual = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Dispozitii?tip={(int)TipDispozitie.Individual}");
            var idsIndividual = doarIndividual.EnumerateArray()
                .Select(d => d.GetProperty("id").GetInt32()).ToList();
            Assert.Contains(idIndividual, idsIndividual);
            Assert.DoesNotContain(idNormativ, idsIndividual);
        }
    }

    // === Izolare tenant ===

    [Fact]
    public async Task Detalii_DispozitieAltTenant_404()
    {
        var admin1 = await AdminNouAsync();
        using (admin1)
        {
            var admin2 = await AdminNouAsync();
            using (admin2)
            {
                var id2 = await admin2.CreeazaDispozitieAsync();
                var raspuns = await admin1.GetAsync($"/api/Dispozitii/{id2}");
                Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
            }
        }
    }

    // === Helpere private ===

    private async Task<HttpClient> AdminNouAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }
}
