using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cleriq.DTOs;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteMentenanta
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteMentenanta(CleriqFixture fixture) => _factory = fixture.Factory;

    private static string CaleDocument(string cheie)
        => Path.Combine(AppContext.BaseDirectory, ConfigTest.CaleRootDocumente, cheie);

    private static string CaleAudio(string cheie)
        => Path.Combine(AppContext.BaseDirectory, ConfigTest.CaleRootAudio, cheie);

    private static void ScrieFisierDirect(string caleFizica)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(caleFizica)!);
        File.WriteAllBytes(caleFizica, Encoding.UTF8.GetBytes("fisier orfan creat direct de test"));
    }

    private static void ImbatranesteFisier(string caleFizica, TimeSpan varsta)
    {
        var moment = DateTime.UtcNow - varsta;
        File.SetCreationTimeUtc(caleFizica, moment);
        File.SetLastWriteTimeUtc(caleFizica, moment);
    }

    [Fact]
    public async Task OrfaniDocumente_ClasificareCorecta_SiActeSemnateProtejate()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        using var admin = await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
        using var superAdmin = await _factory.ClientSuperAdminAsync();

        await admin.CreeazaConsilierAsync("Consilier Unu");
        var sedintaId = await admin.CreeazaSedintaAsync();

        var docViu = await admin.IncarcaDocumentAsync(
            Encoding.UTF8.GetBytes("doc viu"), "viu.pdf", "Document viu", sedintaId: sedintaId);
        var cheieViu = await DbTest.CitesteCaleStocareDocumentAsync(docViu.GetProperty("id").GetInt32());
        ImbatranesteFisier(CaleDocument(cheieViu), TimeSpan.FromHours(2));

        await admin.GenereazaProcesVerbalAsync(sedintaId);
        await admin.FinalizeazaProcesVerbalAsync(sedintaId);
        var uploadSemnat = await admin.IncarcaPvSemnatAsync(
            sedintaId, Encoding.UTF8.GetBytes("%PDF pv semnat"), "pv-semnat.pdf");
        Assert.Equal(HttpStatusCode.OK, uploadSemnat.StatusCode);
        var cheieSemnat = (await DbTest.CitesteCaleStocareSemnatAsync(sedintaId))!;
        ImbatranesteFisier(CaleDocument(cheieSemnat), TimeSpan.FromHours(2));

        // Dispoziție semnată — aceeași stocare fizică; trebuie protejată de scan (PDF de personal)
        var idDispozitie = await admin.CreeazaDispozitieAsync();
        await admin.AtribuieNumarDispozitieAsync(idDispozitie);
        await admin.SemneazaDispozitieAsync(idDispozitie);
        var uploadDisp = await admin.IncarcaDispozitieSemnatAsync(
            idDispozitie, Encoding.UTF8.GetBytes("%PDF dispozitie semnata"), "disp-semnat.pdf");
        Assert.Equal(HttpStatusCode.OK, uploadDisp.StatusCode);
        var cheieDispSemnat = (await DbTest.CitesteCaleStocareSemnatDispozitieAsync(idDispozitie))!;
        ImbatranesteFisier(CaleDocument(cheieDispSemnat), TimeSpan.FromHours(2));

        var cheieOrfan = $"orfan-{Guid.NewGuid():N}.bin";
        ScrieFisierDirect(CaleDocument(cheieOrfan));
        ImbatranesteFisier(CaleDocument(cheieOrfan), TimeSpan.FromHours(2));

        var cheieProaspat = $"orfan-proaspat-{Guid.NewGuid():N}.bin";
        ScrieFisierDirect(CaleDocument(cheieProaspat));

        var docVechi = await admin.IncarcaDocumentAsync(
            Encoding.UTF8.GetBytes("doc sters demult"), "vechi.pdf", "Document vechi", sedintaId: sedintaId);
        var idVechi = docVechi.GetProperty("id").GetInt32();
        var cheieVechi = await DbTest.CitesteCaleStocareDocumentAsync(idVechi);
        Assert.Equal(HttpStatusCode.NoContent,
            (await admin.DeleteAsync($"/api/Documente/{idVechi}")).StatusCode);
        await DbTest.SeteazaStersLaDocumentAsync(idVechi, DateTime.UtcNow.AddDays(-100));
        ImbatranesteFisier(CaleDocument(cheieVechi), TimeSpan.FromHours(2));

        var docRecent = await admin.IncarcaDocumentAsync(
            Encoding.UTF8.GetBytes("doc sters recent"), "recent.pdf", "Document recent", sedintaId: sedintaId);
        var idRecent = docRecent.GetProperty("id").GetInt32();
        var cheieRecent = await DbTest.CitesteCaleStocareDocumentAsync(idRecent);
        Assert.Equal(HttpStatusCode.NoContent,
            (await admin.DeleteAsync($"/api/Documente/{idRecent}")).StatusCode);
        ImbatranesteFisier(CaleDocument(cheieRecent), TimeSpan.FromHours(2));

        var raport = await superAdmin.GetFromJsonAsync<JsonElement>(
            "/api/Mentenanta/OrfaniDocumente?zile=90");
        var chei = raport.GetProperty("fisiere").EnumerateArray()
            .ToDictionary(f => f.GetProperty("cheie").GetString()!,
                          f => f.GetProperty("categorie").GetInt32());

        Assert.Equal(2, raport.GetProperty("totalFisiere").GetInt32());
        Assert.Equal((int)CategorieOrfan.FaraRandInDb, chei[cheieOrfan]);
        Assert.Equal((int)CategorieOrfan.SoftDeletedVechi, chei[cheieVechi]);
        Assert.False(chei.ContainsKey(cheieViu));
        Assert.False(chei.ContainsKey(cheieSemnat));
        Assert.False(chei.ContainsKey(cheieDispSemnat));
        Assert.False(chei.ContainsKey(cheieProaspat));
        Assert.False(chei.ContainsKey(cheieRecent));

        var stergere = await superAdmin.PostAsync("/api/Mentenanta/OrfaniDocumente?zile=90", null);
        Assert.Equal(HttpStatusCode.OK, stergere.StatusCode);
        var rezultat = await stergere.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, rezultat.GetProperty("sterse").GetInt32());
        Assert.Equal(0, rezultat.GetProperty("esuate").GetInt32());
        Assert.Equal(1, rezultat.GetProperty("sterseFaraRandInDb").GetInt32());
        Assert.Equal(1, rezultat.GetProperty("sterseSoftDeletedVechi").GetInt32());

        Assert.False(File.Exists(CaleDocument(cheieOrfan)));
        Assert.False(File.Exists(CaleDocument(cheieVechi)));
        Assert.True(File.Exists(CaleDocument(cheieViu)));
        Assert.True(File.Exists(CaleDocument(cheieSemnat)));
        Assert.True(File.Exists(CaleDocument(cheieDispSemnat)));
        Assert.True(File.Exists(CaleDocument(cheieProaspat)));
        Assert.True(File.Exists(CaleDocument(cheieRecent)));

        var raportFinal = await superAdmin.GetFromJsonAsync<JsonElement>(
            "/api/Mentenanta/OrfaniDocumente?zile=90");
        Assert.Equal(0, raportFinal.GetProperty("totalFisiere").GetInt32());
    }

    [Fact]
    public async Task OrfaniAudio_ClasificareCorectaSiStergere()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        using var admin = await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
        using var superAdmin = await _factory.ClientSuperAdminAsync();

        var sedintaViuId = await admin.CreeazaSedintaAsync("Ședință audio viu");
        var uploadViu = await admin.IncarcaAudioAsync(
            sedintaViuId, Encoding.UTF8.GetBytes("audio viu"), "viu.mp3");
        Assert.Equal(HttpStatusCode.OK, uploadViu.StatusCode);
        var cheieViu = await DbTest.CitesteCaleStocareAudioAsync(sedintaViuId);
        ImbatranesteFisier(CaleAudio(cheieViu), TimeSpan.FromHours(2));

        var cheieOrfan = $"orfan-audio-{Guid.NewGuid():N}.mp3";
        ScrieFisierDirect(CaleAudio(cheieOrfan));
        ImbatranesteFisier(CaleAudio(cheieOrfan), TimeSpan.FromHours(2));

        var sedintaVechiId = await admin.CreeazaSedintaAsync("Ședință audio vechi");
        var uploadVechi = await admin.IncarcaAudioAsync(
            sedintaVechiId, Encoding.UTF8.GetBytes("audio sters demult"), "vechi.mp3");
        Assert.Equal(HttpStatusCode.OK, uploadVechi.StatusCode);
        var idVechi = (await uploadVechi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt32();
        var cheieVechi = await DbTest.CitesteCaleStocareAudioAsync(sedintaVechiId);
        Assert.Equal(HttpStatusCode.NoContent,
            (await admin.DeleteAsync($"/api/Sedinte/{sedintaVechiId}/Transcriere")).StatusCode);
        await DbTest.SeteazaStersLaTranscriereAsync(idVechi, DateTime.UtcNow.AddDays(-100));
        ImbatranesteFisier(CaleAudio(cheieVechi), TimeSpan.FromHours(2));

        var raport = await superAdmin.GetFromJsonAsync<JsonElement>(
            "/api/Mentenanta/OrfaniAudio?zile=90");
        var chei = raport.GetProperty("fisiere").EnumerateArray()
            .ToDictionary(f => f.GetProperty("cheie").GetString()!,
                          f => f.GetProperty("categorie").GetInt32());

        Assert.Equal(2, raport.GetProperty("totalFisiere").GetInt32());
        Assert.Equal((int)CategorieOrfan.FaraRandInDb, chei[cheieOrfan]);
        Assert.Equal((int)CategorieOrfan.SoftDeletedVechi, chei[cheieVechi]);
        Assert.False(chei.ContainsKey(cheieViu));

        var stergere = await superAdmin.PostAsync("/api/Mentenanta/OrfaniAudio?zile=90", null);
        Assert.Equal(HttpStatusCode.OK, stergere.StatusCode);
        var rezultat = await stergere.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, rezultat.GetProperty("sterse").GetInt32());
        Assert.Equal(0, rezultat.GetProperty("esuate").GetInt32());

        Assert.False(File.Exists(CaleAudio(cheieOrfan)));
        Assert.False(File.Exists(CaleAudio(cheieVechi)));
        Assert.True(File.Exists(CaleAudio(cheieViu)));
    }

    [Fact]
    public async Task Mentenanta_PragMinim30Zile_SiDoarSuperAdmin()
    {
        using var superAdmin = await _factory.ClientSuperAdminAsync();

        Assert.Equal(HttpStatusCode.BadRequest,
            (await superAdmin.GetAsync("/api/Mentenanta/OrfaniDocumente?zile=10")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await superAdmin.PostAsync("/api/Mentenanta/OrfaniAudio?zile=29", null)).StatusCode);

        var inst = await _factory.ProvisioneazaInstitutieAsync();
        using var admin = await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await admin.GetAsync("/api/Mentenanta/OrfaniDocumente")).StatusCode);
    }
}