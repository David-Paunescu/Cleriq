using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteHcl
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteHcl(CleriqFixture fixture) => _factory = fixture.Factory;

    // === Generare ===

    [Fact]
    public async Task Genereaza_DinPunctAdoptat_CreeazaDraftCuSemnatariSiContinut()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();

            var detalii = await admin.GetFromJsonAsync<JsonElement>($"/api/Hcl/{hcl.HclId}");
            Assert.Equal((int)StatusHclRedactional.Draft, detalii.GetProperty("status").GetInt32());
            Assert.Equal("Punct 1", detalii.GetProperty("titlu").GetString());
            Assert.Equal(1, detalii.GetProperty("votPentru").GetInt32());
            Assert.Equal(0, detalii.GetProperty("votImpotriva").GetInt32());
            Assert.Equal(JsonValueKind.Null, detalii.GetProperty("numar").ValueKind);

            var continut = detalii.GetProperty("continut").GetString()!;
            Assert.Contains("HOTĂRÂREA Nr.", continut);
            Assert.Contains("_[urmează să fie atribuit]_", continut); // placeholder pe Draft
            Assert.Contains("Secretar general", continut);

            var semnatari = detalii.GetProperty("semnatari").EnumerateArray().ToList();
            Assert.Equal(2, semnatari.Count);
            Assert.Contains(semnatari, s =>
                s.GetProperty("rolSemnatar").GetInt32() == (int)RolSemnatar.PresedinteSedinta
                && s.GetProperty("consilierId").ValueKind != JsonValueKind.Null);
            Assert.Contains(semnatari, s =>
                s.GetProperty("rolSemnatar").GetInt32() == (int)RolSemnatar.SecretarUat
                && s.GetProperty("persoanaId").ValueKind != JsonValueKind.Null);
        }
    }

    [Fact]
    public async Task Genereaza_FaraPresedinteSedinta_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var (_, punctId, _) = await PunctAdoptatAsync(admin);
            var raspuns = await admin.PostAsJsonAsync("/api/Hcl/Genereaza",
                new { punctOrdineZiId = punctId, tipHcl = TipHcl.Normativ });
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Genereaza_PunctNeadoptat_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var sedintaId = await admin.CreeazaSedintaAsync();
            var punctId = await admin.CreeazaPunctAsync(sedintaId, TipMajoritate.Simpla);
            // punctul nu a fost votat/închis → Rezultat null

            var raspuns = await admin.PostAsJsonAsync("/api/Hcl/Genereaza",
                new { punctOrdineZiId = punctId, tipHcl = TipHcl.Normativ });
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Genereaza_FaraSecretarUat_400()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var (sedintaId, punctId, consilierId) = await PunctAdoptatAsync(admin);
            var rPres = await admin.PostAsJsonAsync(
                $"/api/Sedinte/{sedintaId}/PresedinteSedinta", new { consilierId });
            Assert.True(rPres.IsSuccessStatusCode, await rPres.Content.ReadAsStringAsync());

            // niciun mandat Secretar UAT activ la data adoptării → precondiția cade
            var raspuns = await admin.PostAsJsonAsync("/api/Hcl/Genereaza",
                new { punctOrdineZiId = punctId, tipHcl = TipHcl.Normativ });
            Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Genereaza_DePeAcelasiPunct_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var raspuns = await admin.PostAsJsonAsync("/api/Hcl/Genereaza",
                new { punctOrdineZiId = hcl.PunctId, tipHcl = TipHcl.Normativ });
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    // === Legătura punct → HCL (hclId pe DTO-ul de punct) ===

    [Fact]
    public async Task PuncteLista_ExpuneHclId_DoarPePunctulCuHcl()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var punctFaraHclId = await admin.CreeazaPunctAsync(
                hcl.SedintaId, TipMajoritate.Simpla, ordine: 2);

            var puncte = await admin.GetFromJsonAsync<JsonElement>(
                $"/api/Sedinte/{hcl.SedintaId}/Puncte");

            var punctCuHcl = puncte.EnumerateArray()
                .Single(p => p.GetProperty("id").GetInt32() == hcl.PunctId);
            Assert.Equal(hcl.HclId, punctCuHcl.GetProperty("hclId").GetInt32());

            var punctFaraHcl = puncte.EnumerateArray()
                .Single(p => p.GetProperty("id").GetInt32() == punctFaraHclId);
            Assert.Equal(JsonValueKind.Null, punctFaraHcl.GetProperty("hclId").ValueKind);
        }
    }

    // === Conținut + semnare ===

    [Fact]
    public async Task Continut_EditabilPeDraft_BlocatDupaSemnare()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();

            var editare = await admin.PutAsJsonAsync($"/api/Hcl/{hcl.HclId}/Continut",
                new { continut = "## HOTĂRĂȘTE:\n\nArt. 1. Text editat de secretar." });
            Assert.Equal(HttpStatusCode.OK, editare.StatusCode);

            var detalii = await admin.GetFromJsonAsync<JsonElement>($"/api/Hcl/{hcl.HclId}");
            Assert.Contains("Text editat de secretar", detalii.GetProperty("continut").GetString());

            await admin.AtribuieNumarHclAsync(hcl.HclId);
            await admin.SemneazaHclAsync(hcl.HclId);

            var editareDupaSemnare = await admin.PutAsJsonAsync(
                $"/api/Hcl/{hcl.HclId}/Continut", new { continut = "alt text" });
            Assert.Equal(HttpStatusCode.Conflict, editareDupaSemnare.StatusCode);

            var regenerare = await admin.PostAsync($"/api/Hcl/{hcl.HclId}/RegenereazaContinut", null);
            Assert.Equal(HttpStatusCode.Conflict, regenerare.StatusCode);
        }
    }

    [Fact]
    public async Task Semneaza_BlocatPeDraft_PermisPeNumerotat()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();

            var peDraft = await admin.PostAsync($"/api/Hcl/{hcl.HclId}/Semneaza", null);
            Assert.Equal(HttpStatusCode.Conflict, peDraft.StatusCode);

            await admin.AtribuieNumarHclAsync(hcl.HclId);

            var peNumerotat = await admin.PostAsync($"/api/Hcl/{hcl.HclId}/Semneaza", null);
            Assert.Equal(HttpStatusCode.OK, peNumerotat.StatusCode);
            var dto = await peNumerotat.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((int)StatusHclRedactional.Semnat, dto.GetProperty("status").GetInt32());
        }
    }

    [Fact]
    public async Task Mutatii_IntorcDetaliiComplete_CuContinutSiSemnatari()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();

            // EditeazaContinut → întoarce Detalii (continut + semnatari în răspuns)
            var editare = await admin.PutAsJsonAsync($"/api/Hcl/{hcl.HclId}/Continut",
                new { continut = "## HOTĂRĂȘTE:\n\nArt. 1. Text de test." });
            Assert.Equal(HttpStatusCode.OK, editare.StatusCode);
            var dtoEdit = await editare.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains("Text de test", dtoEdit.GetProperty("continut").GetString()!);
            Assert.Equal(2, dtoEdit.GetProperty("semnatari").EnumerateArray().Count());

            // RegenereazaContinut → noul conținut vine direct în răspuns
            var regen = await admin.PostAsync($"/api/Hcl/{hcl.HclId}/RegenereazaContinut", null);
            Assert.Equal(HttpStatusCode.OK, regen.StatusCode);
            var dtoRegen = await regen.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains("HOTĂRÂREA Nr.", dtoRegen.GetProperty("continut").GetString()!);

            // AtribuieNumar → răspunsul conține conținutul cu placeholderul înlocuit
            var atribuie = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/AtribuieNumar",
                new { numar = 7, confirmaCuLacune = true });
            Assert.Equal(HttpStatusCode.OK, atribuie.StatusCode);
            var dtoNumar = await atribuie.Content.ReadFromJsonAsync<JsonElement>();
            var an = dtoNumar.GetProperty("anNumerotare").GetInt32();
            var continutNumerotat = dtoNumar.GetProperty("continut").GetString()!;
            Assert.Contains($"7/{an}", continutNumerotat);
            Assert.DoesNotContain("_[urmează să fie atribuit]_", continutNumerotat);

            // Semneaza → întoarce Detalii cu status Semnat
            var semnare = await admin.PostAsync($"/api/Hcl/{hcl.HclId}/Semneaza", null);
            Assert.Equal(HttpStatusCode.OK, semnare.StatusCode);
            var dtoSemn = await semnare.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal((int)StatusHclRedactional.Semnat, dtoSemn.GetProperty("status").GetInt32());
            Assert.Equal(2, dtoSemn.GetProperty("semnatari").EnumerateArray().Count());
        }
    }

    // === PDF generat + variantă semnată ===

    [Fact]
    public async Task Pdf_ReturneazaPdfValid()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var raspuns = await admin.GetAsync($"/api/Hcl/{hcl.HclId}/Pdf");

            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
            Assert.Equal("application/pdf", raspuns.Content.Headers.ContentType!.MediaType);
            var bytes = await raspuns.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.Length > 100);
            Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        }
    }

    [Fact]
    public async Task Semnat_UploadDescarcareReplaceStergere()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            await admin.SemneazaHclAsync(hcl.HclId);

            var bytesV1 = Encoding.UTF8.GetBytes("%PDF-1.4 hcl semnat unu");
            var upload = await admin.IncarcaHclSemnatAsync(hcl.HclId, bytesV1, "hcl-semnat-v1.pdf");
            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
            var dto = await upload.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(dto.GetProperty("esteSemnat").GetBoolean());
            Assert.Equal("hcl-semnat-v1.pdf", dto.GetProperty("numeFisierSemnat").GetString());
            Assert.Equal((long)bytesV1.Length, dto.GetProperty("marimeSemnat").GetInt64());

            var descarcare = await admin.GetAsync($"/api/Hcl/{hcl.HclId}/Semnat");
            Assert.Equal(HttpStatusCode.OK, descarcare.StatusCode);
            Assert.Equal("application/pdf", descarcare.Content.Headers.ContentType!.MediaType);
            Assert.Equal(bytesV1, await descarcare.Content.ReadAsByteArrayAsync());

            var bytesV2 = Encoding.UTF8.GetBytes("%PDF-1.4 hcl semnat doi, mai lung");
            var replace = await admin.IncarcaHclSemnatAsync(hcl.HclId, bytesV2, "hcl-semnat-v2.pdf");
            Assert.Equal(HttpStatusCode.OK, replace.StatusCode);
            var dupaReplace = await admin.GetAsync($"/api/Hcl/{hcl.HclId}/Semnat");
            Assert.Equal(bytesV2, await dupaReplace.Content.ReadAsByteArrayAsync());

            var stergere = await admin.DeleteAsync($"/api/Hcl/{hcl.HclId}/Semnat");
            Assert.Equal(HttpStatusCode.NoContent, stergere.StatusCode);

            var dupaStergere = await admin.GetAsync($"/api/Hcl/{hcl.HclId}/Semnat");
            Assert.Equal(HttpStatusCode.NotFound, dupaStergere.StatusCode);
        }
    }

    [Fact]
    public async Task Semnat_PeHclNesemnat_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync(); // rămâne Draft
            var raspuns = await admin.IncarcaHclSemnatAsync(
                hcl.HclId, Encoding.UTF8.GetBytes("%PDF-1.4"), "x.pdf");
            Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task Semnat_VariantaB_PrimaAtasarePostMolOk_ReplaceSiStergere_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            await admin.SemneazaHclAsync(hcl.HclId);

            var mol = await admin.PutAsJsonAsync($"/api/Hcl/{hcl.HclId}/PublicareMol",
                new { dataPublicareMol = DateTime.UtcNow });
            Assert.Equal(HttpStatusCode.OK, mol.StatusCode);

            // prima atașare post-MOL e permisă (upgrade benign — varianta B)
            var prima = await admin.IncarcaHclSemnatAsync(
                hcl.HclId, Encoding.UTF8.GetBytes("%PDF-1.4 prima"), "hcl-semnat.pdf");
            Assert.Equal(HttpStatusCode.OK, prima.StatusCode);

            // replace după MOL → 409 (dovada deja publicată e intangibilă)
            var replace = await admin.IncarcaHclSemnatAsync(
                hcl.HclId, Encoding.UTF8.GetBytes("%PDF-1.4 replace"), "hcl-semnat-2.pdf");
            Assert.Equal(HttpStatusCode.Conflict, replace.StatusCode);

            // DELETE după MOL → 409
            var stergere = await admin.DeleteAsync($"/api/Hcl/{hcl.HclId}/Semnat");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    // === Ștergere (matricea de gărzi) ===

    [Fact]
    public async Task Sterge_DraftFaraDependinte_204()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            var stergere = await admin.DeleteAsync($"/api/Hcl/{hcl.HclId}");
            Assert.Equal(HttpStatusCode.NoContent, stergere.StatusCode);

            var dupa = await admin.GetAsync($"/api/Hcl/{hcl.HclId}");
            Assert.Equal(HttpStatusCode.NotFound, dupa.StatusCode);
        }
    }

    [Fact]
    public async Task Sterge_Semnat_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            await admin.SemneazaHclAsync(hcl.HclId);

            var stergere = await admin.DeleteAsync($"/api/Hcl/{hcl.HclId}");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    [Fact]
    public async Task Sterge_Publicat_409()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);

            var publicare = await admin.PutAsJsonAsync(
                $"/api/Hcl/{hcl.HclId}/Publicare", new { estePublicat = true });
            Assert.Equal(HttpStatusCode.OK, publicare.StatusCode);

            var stergere = await admin.DeleteAsync($"/api/Hcl/{hcl.HclId}");
            Assert.Equal(HttpStatusCode.Conflict, stergere.StatusCode);
        }
    }

    [Fact]
    public async Task Sterge_Invalidat_204_OverrideChiarSiPeSemnat()
    {
        var admin = await AdminNouAsync();
        using (admin)
        {
            var hcl = await admin.GenereazaHclAdoptatAsync();
            await admin.AtribuieNumarHclAsync(hcl.HclId);
            await admin.SemneazaHclAsync(hcl.HclId);

            var invalidare = await admin.PostAsJsonAsync($"/api/Hcl/{hcl.HclId}/Invalidare",
                new
                {
                    motiv = MotivInvalidare.AnulatInstanta,
                    refInvalidare = "Sent. civ. 100/2026",
                    confirmaCuRelatiiActive = false
                });
            Assert.Equal(HttpStatusCode.OK, invalidare.StatusCode);

            // invalidat → garda override permite ștergerea chiar și a unui act semnat
            var stergere = await admin.DeleteAsync($"/api/Hcl/{hcl.HclId}");
            Assert.Equal(HttpStatusCode.NoContent, stergere.StatusCode);
        }
    }

    // === Izolare tenant ===

    [Fact]
    public async Task Detalii_HclAltTenant_404()
    {
        var admin1 = await AdminNouAsync();
        using (admin1)
        {
            var admin2 = await AdminNouAsync();
            using (admin2)
            {
                var hcl2 = await admin2.GenereazaHclAdoptatAsync();
                var raspuns = await admin1.GetAsync($"/api/Hcl/{hcl2.HclId}");
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

    // Punct ProiectHCL adoptat pe o ședință convocată, FĂRĂ președinte/secretar setați —
    // pentru testele negative de precondiții la Genereaza.
    private static async Task<(int SedintaId, int PunctId, int ConsilierId)> PunctAdoptatAsync(
        HttpClient admin)
    {
        var consilierId = await admin.CreeazaConsilierAsync("Consilier 01");
        var sedintaId = await admin.CreeazaSedintaAsync();
        await admin.SeteazaPrezentaAsync(sedintaId, consilierId, StatusPrezenta.Prezent);
        var punctId = await admin.CreeazaPunctAsync(sedintaId, TipMajoritate.Simpla);
        await admin.VoteazaAsync(sedintaId, punctId, consilierId, OptiuneVot.Pentru);
        await admin.InchideVotAsync(sedintaId, punctId);
        await DbTest.SeteazaStatusSedintaAsync(sedintaId, StatusSedinta.Convocata);
        return (sedintaId, punctId, consilierId);
    }
}
