using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.DTOs;
using Cleriq.Models;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteConvocare
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteConvocare(CleriqFixture fixture) => _factory = fixture.Factory;

    private async Task<HttpClient> AdminAsync()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        return await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
    }

    private static JsonElement Convocarea(JsonElement lista, string numeConsilier)
        => lista.EnumerateArray()
            .First(c => c.GetProperty("numeCompletConsilier").GetString() == numeConsilier);

    [Fact]
    public async Task PostConvocare_ProfileMixteDeCoordonate_StatusuriCorectePerCanal()
    {
        using var admin = await AdminAsync();
        await admin.CreeazaConsilierAsync("Ana Ambele", "ana@test.ro", "0720111222");
        await admin.CreeazaConsilierAsync("Bogdan DoarEmail", "bogdan@test.ro");
        await admin.CreeazaConsilierAsync("Costel FaraNimic");
        var sedintaId = await admin.CreeazaSedintaAsync();

        var rezultat = await admin.TrimiteConvocariAsync(sedintaId);

        Assert.Equal(3, rezultat.GetProperty("totalConsilieri").GetInt32());
        Assert.Equal(2, rezultat.GetProperty("inCursDeTrimitere").GetInt32());
        Assert.Equal(1, rezultat.GetProperty("faraCoordonate").GetInt32());
        Assert.Equal(0, rezultat.GetProperty("totalSucces").GetInt32());

        var lista = await admin.ListaConvocariAsync(sedintaId);

        var ana = Convocarea(lista, "Ana Ambele");
        Assert.Equal((int)StatusTrimitere.InAsteptare, ana.GetProperty("emailStatus").GetInt32());
        Assert.Equal((int)StatusTrimitere.InAsteptare, ana.GetProperty("smsStatus").GetInt32());
        Assert.Equal((int)StatusConvocare.InCursDeTrimitere, ana.GetProperty("statusGeneral").GetInt32());

        var bogdan = Convocarea(lista, "Bogdan DoarEmail");
        Assert.Equal((int)StatusTrimitere.InAsteptare, bogdan.GetProperty("emailStatus").GetInt32());
        Assert.Equal((int)StatusTrimitere.FaraDestinatie, bogdan.GetProperty("smsStatus").GetInt32());
        Assert.Equal((int)StatusConvocare.InCursDeTrimitere, bogdan.GetProperty("statusGeneral").GetInt32());

        var costel = Convocarea(lista, "Costel FaraNimic");
        Assert.Equal((int)StatusTrimitere.FaraDestinatie, costel.GetProperty("emailStatus").GetInt32());
        Assert.Equal((int)StatusTrimitere.FaraDestinatie, costel.GetProperty("smsStatus").GetInt32());
        Assert.Equal((int)StatusConvocare.FaraCoordonate, costel.GetProperty("statusGeneral").GetInt32());

        var sedinta = await admin.GetFromJsonAsync<JsonElement>($"/api/Sedinte/{sedintaId}");
        Assert.Equal((int)StatusSedinta.Convocata, sedinta.GetProperty("status").GetInt32());
        Assert.NotEqual(JsonValueKind.Null, sedinta.GetProperty("convocareTrimisaLa").ValueKind);
    }

    [Fact]
    public async Task PostConvocare_ConsilierInactiv_NuEsteConvocat()
    {
        using var admin = await AdminAsync();
        await admin.CreeazaConsilierAsync("Activ Unu", "activ@test.ro");
        var inactivId = await admin.CreeazaConsilierAsync("Inactiv Doi", "inactiv@test.ro");
        var dezactivare = await admin.PutAsJsonAsync($"/api/Consilieri/{inactivId}",
            new { numeComplet = "Inactiv Doi", email = "inactiv@test.ro", telefon = (string?)null, activ = false });
        Assert.Equal(HttpStatusCode.OK, dezactivare.StatusCode);

        var sedintaId = await admin.CreeazaSedintaAsync();
        var rezultat = await admin.TrimiteConvocariAsync(sedintaId);

        Assert.Equal(1, rezultat.GetProperty("totalConsilieri").GetInt32());
        var lista = await admin.ListaConvocariAsync(sedintaId);
        Assert.Equal(1, lista.GetArrayLength());
        Assert.Equal("Activ Unu", lista[0].GetProperty("numeCompletConsilier").GetString());
    }

    [Fact]
    public async Task RePost_TrimisaRamane_EsuataDevineInAsteptare_TimestampEmitereNeschimbat()
    {
        using var admin = await AdminAsync();
        await admin.CreeazaConsilierAsync("Ana Ambele", "ana@test.ro", "0720111222");
        var sedintaId = await admin.CreeazaSedintaAsync();

        var primul = await admin.TrimiteConvocariAsync(sedintaId);
        var emisLa = primul.GetProperty("convocareTrimisaLa").GetDateTime();

        var convocareId = (await admin.ListaConvocariAsync(sedintaId))[0].GetProperty("id").GetInt32();
        await DbTest.SeteazaStatusuriConvocareAsync(convocareId,
            emailStatus: StatusTrimitere.Trimisa, smsStatus: StatusTrimitere.Esuata);

        var alDoilea = await admin.TrimiteConvocariAsync(sedintaId);
        Assert.Equal(emisLa, alDoilea.GetProperty("convocareTrimisaLa").GetDateTime());

        var dupa = (await admin.ListaConvocariAsync(sedintaId))[0];
        Assert.Equal((int)StatusTrimitere.Trimisa, dupa.GetProperty("emailStatus").GetInt32());
        Assert.NotEqual(JsonValueKind.Null, dupa.GetProperty("emailTrimisLa").ValueKind);
        Assert.Equal((int)StatusTrimitere.InAsteptare, dupa.GetProperty("smsStatus").GetInt32());
        Assert.Equal(JsonValueKind.Null, dupa.GetProperty("smsTrimisLa").ValueKind);
        Assert.Equal(JsonValueKind.Null, dupa.GetProperty("smsDetalii").ValueKind);
    }

    [Fact]
    public async Task RePost_EmailStersDinProfil_IstoriculTrimisaSePastreaza()
    {
        using var admin = await AdminAsync();
        var consilierId = await admin.CreeazaConsilierAsync("Bogdan Istoric", "bogdan@test.ro");
        var sedintaId = await admin.CreeazaSedintaAsync();
        await admin.TrimiteConvocariAsync(sedintaId);

        var convocareId = (await admin.ListaConvocariAsync(sedintaId))[0].GetProperty("id").GetInt32();
        await DbTest.SeteazaStatusuriConvocareAsync(convocareId, emailStatus: StatusTrimitere.Trimisa);

        var actualizare = await admin.PutAsJsonAsync($"/api/Consilieri/{consilierId}",
            new { numeComplet = "Bogdan Istoric", email = (string?)null, telefon = (string?)null, activ = true });
        Assert.Equal(HttpStatusCode.OK, actualizare.StatusCode);

        await admin.TrimiteConvocariAsync(sedintaId);

        var dupa = (await admin.ListaConvocariAsync(sedintaId))[0];
        Assert.Equal((int)StatusTrimitere.Trimisa, dupa.GetProperty("emailStatus").GetInt32());
    }

    [Fact]
    public async Task PostConvocare_SedintaAnulata_Returneaza409()
    {
        using var admin = await AdminAsync();
        await admin.CreeazaConsilierAsync("Consilier Unu", "c1@test.ro");
        var sedintaId = await admin.CreeazaSedintaAsync();
        await admin.TranziteazaSedintaAsync(sedintaId, "Anuleaza");

        var raspuns = await admin.PostAsync($"/api/Sedinte/{sedintaId}/Convocare", null);
        Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
    }

    [Fact]
    public async Task PostConvocare_FaraConsilieriActivi_Returneaza400()
    {
        using var admin = await AdminAsync();
        var sedintaId = await admin.CreeazaSedintaAsync();

        var raspuns = await admin.PostAsync($"/api/Sedinte/{sedintaId}/Convocare", null);
        Assert.Equal(HttpStatusCode.BadRequest, raspuns.StatusCode);
    }

    [Fact]
    public async Task RePost_DinInDesfasurare_NuRetrogradeazaStatusul()
    {
        using var admin = await AdminAsync();
        await admin.CreeazaConsilierAsync("Consilier Unu", "c1@test.ro");
        var sedintaId = await admin.CreeazaSedintaAsync();
        await admin.TrimiteConvocariAsync(sedintaId);
        await admin.TranziteazaSedintaAsync(sedintaId, "Incepe");

        await admin.TrimiteConvocariAsync(sedintaId);

        var sedinta = await admin.GetFromJsonAsync<JsonElement>($"/api/Sedinte/{sedintaId}");
        Assert.Equal((int)StatusSedinta.InDesfasurare, sedinta.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task DeleteConvocare_ReseteazaSedinta_IarRePostRestaureazaAceleasiRanduri()
    {
        using var admin = await AdminAsync();
        await admin.CreeazaConsilierAsync("Ana Ambele", "ana@test.ro", "0720111222");
        await admin.CreeazaConsilierAsync("Costel FaraNimic");
        var sedintaId = await admin.CreeazaSedintaAsync();
        await admin.TrimiteConvocariAsync(sedintaId);

        var inainte = await admin.ListaConvocariAsync(sedintaId);
        var idsInainte = inainte.EnumerateArray()
            .ToDictionary(c => c.GetProperty("numeCompletConsilier").GetString()!,
                          c => c.GetProperty("id").GetInt32());

        var stergere = await admin.DeleteAsync($"/api/Sedinte/{sedintaId}/Convocare");
        Assert.Equal(HttpStatusCode.NoContent, stergere.StatusCode);

        Assert.Equal(0, (await admin.ListaConvocariAsync(sedintaId)).GetArrayLength());
        var sedinta = await admin.GetFromJsonAsync<JsonElement>($"/api/Sedinte/{sedintaId}");
        Assert.Equal((int)StatusSedinta.Planificata, sedinta.GetProperty("status").GetInt32());
        Assert.Equal(JsonValueKind.Null, sedinta.GetProperty("convocareTrimisaLa").ValueKind);

        await admin.TrimiteConvocariAsync(sedintaId);
        var dupa = await admin.ListaConvocariAsync(sedintaId);
        Assert.Equal(2, dupa.GetArrayLength());

        foreach (var co in dupa.EnumerateArray())
        {
            var nume = co.GetProperty("numeCompletConsilier").GetString()!;
            Assert.Equal(idsInainte[nume], co.GetProperty("id").GetInt32());
        }
        Assert.Equal((int)StatusConvocare.InCursDeTrimitere,
            Convocarea(dupa, "Ana Ambele").GetProperty("statusGeneral").GetInt32());
        Assert.Equal((int)StatusConvocare.FaraCoordonate,
            Convocarea(dupa, "Costel FaraNimic").GetProperty("statusGeneral").GetInt32());
    }

    [Fact]
    public async Task DeleteConvocare_SedintaInDesfasurare_Returneaza409()
    {
        using var admin = await AdminAsync();
        await admin.CreeazaConsilierAsync("Consilier Unu", "c1@test.ro");
        var sedintaId = await admin.CreeazaSedintaAsync();
        await admin.TrimiteConvocariAsync(sedintaId);
        await admin.TranziteazaSedintaAsync(sedintaId, "Incepe");

        var raspuns = await admin.DeleteAsync($"/api/Sedinte/{sedintaId}/Convocare");
        Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
    }

    [Fact]
    public async Task Retry_EsuataDevineInAsteptare_TrimisaRamane_FaraIncercariDinController()
    {
        using var admin = await AdminAsync();
        await admin.CreeazaConsilierAsync("Ana Ambele", "ana@test.ro", "0720111222");
        var sedintaId = await admin.CreeazaSedintaAsync();
        await admin.TrimiteConvocariAsync(sedintaId);

        var convocareId = (await admin.ListaConvocariAsync(sedintaId))[0].GetProperty("id").GetInt32();
        await DbTest.SeteazaStatusuriConvocareAsync(convocareId,
            emailStatus: StatusTrimitere.Esuata, smsStatus: StatusTrimitere.Trimisa);

        var raspuns = await admin.PostAsync(
            $"/api/Sedinte/{sedintaId}/Convocari/{convocareId}/Retry", null);
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

        var dto = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)StatusTrimitere.InAsteptare, dto.GetProperty("emailStatus").GetInt32());
        Assert.Equal(JsonValueKind.Null, dto.GetProperty("emailTrimisLa").ValueKind);
        Assert.Equal((int)StatusTrimitere.Trimisa, dto.GetProperty("smsStatus").GetInt32());

        var incercari = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/Sedinte/{sedintaId}/Convocari/{convocareId}/Incercari");
        Assert.Equal(0, incercari.GetArrayLength());
    }

    [Fact]
    public async Task Retry_CoordonateAdaugateUlterior_FaraDestinatieDevineInAsteptare()
    {
        using var admin = await AdminAsync();
        var consilierId = await admin.CreeazaConsilierAsync("Costel Tarziu");
        var sedintaId = await admin.CreeazaSedintaAsync();
        await admin.TrimiteConvocariAsync(sedintaId);

        var convocare = (await admin.ListaConvocariAsync(sedintaId))[0];
        Assert.Equal((int)StatusConvocare.FaraCoordonate, convocare.GetProperty("statusGeneral").GetInt32());
        var convocareId = convocare.GetProperty("id").GetInt32();

        var actualizare = await admin.PutAsJsonAsync($"/api/Consilieri/{consilierId}",
            new { numeComplet = "Costel Tarziu", email = "costel@test.ro", telefon = (string?)null, activ = true });
        Assert.Equal(HttpStatusCode.OK, actualizare.StatusCode);

        var raspuns = await admin.PostAsync(
            $"/api/Sedinte/{sedintaId}/Convocari/{convocareId}/Retry", null);
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

        var dto = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)StatusTrimitere.InAsteptare, dto.GetProperty("emailStatus").GetInt32());
        Assert.Equal((int)StatusTrimitere.FaraDestinatie, dto.GetProperty("smsStatus").GetInt32());
    }

    // === Dispoziția de convocare (Pas 12) ===

    [Fact]
    public async Task Convocare_CuMandate_GenereazaDispozitiaDeConvocare_DraftIndividualaLegata()
    {
        using var admin = await AdminAsync();
        var azi = DateOnly.FromDateTime(DateTime.UtcNow);
        await admin.AsigurareMandatPrimarAsync(azi);
        await admin.AsigurareMandatSecretarAsync(azi);
        await admin.CreeazaConsilierAsync("Ana Consilier", "ana@test.ro");
        var sedintaId = await admin.CreeazaSedintaAsync();
        await admin.CreeazaPunctAsync(sedintaId, TipMajoritate.Simpla, ordine: 1);

        await admin.TrimiteConvocariAsync(sedintaId);

        // dispoziția de convocare legată de ședință apare în lista de dispoziții
        var lista = await admin.GetFromJsonAsync<JsonElement>("/api/Dispozitii");
        var disp = lista.EnumerateArray().First(d =>
            d.GetProperty("sedintaId").ValueKind != JsonValueKind.Null
            && d.GetProperty("sedintaId").GetInt32() == sedintaId);

        Assert.Equal((int)StatusActRedactional.Draft, disp.GetProperty("status").GetInt32());
        Assert.Equal((int)TipDispozitie.Individual, disp.GetProperty("tipDispozitie").GetInt32());
        Assert.False(disp.GetProperty("estePublicat").GetBoolean());

        // conținutul are formula de convocare, temeiul și punctul din ordinea de zi
        var detalii = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/Dispozitii/{disp.GetProperty("id").GetInt32()}");
        var continut = detalii.GetProperty("continut").GetString()!;
        Assert.Contains("Se convoacă", continut);
        Assert.Contains("art. 134 alin. (1) lit. a)", continut);
        Assert.Contains("Punct 1", continut);
    }

    [Fact]
    public async Task Convocare_FaraMandate_NuGenereazaDispozitie_DarConvocareaReuseste()
    {
        using var admin = await AdminAsync();
        await admin.CreeazaConsilierAsync("Ana Consilier", "ana@test.ro");
        var sedintaId = await admin.CreeazaSedintaAsync();

        // best-effort: fără mandate de primar/secretar, convocarea reușește oricum
        var rezultat = await admin.TrimiteConvocariAsync(sedintaId);
        Assert.Equal(1, rezultat.GetProperty("totalConsilieri").GetInt32());

        var lista = await admin.GetFromJsonAsync<JsonElement>("/api/Dispozitii");
        var sedinteLegate = lista.EnumerateArray()
            .Where(d => d.GetProperty("sedintaId").ValueKind != JsonValueKind.Null)
            .Select(d => d.GetProperty("sedintaId").GetInt32())
            .ToList();
        Assert.DoesNotContain(sedintaId, sedinteLegate);
    }

    [Fact]
    public async Task Convocare_RePost_NuDublezaDispozitiaDeConvocare()
    {
        using var admin = await AdminAsync();
        var azi = DateOnly.FromDateTime(DateTime.UtcNow);
        await admin.AsigurareMandatPrimarAsync(azi);
        await admin.AsigurareMandatSecretarAsync(azi);
        await admin.CreeazaConsilierAsync("Ana Consilier", "ana@test.ro");
        var sedintaId = await admin.CreeazaSedintaAsync();

        await admin.TrimiteConvocariAsync(sedintaId);
        await admin.TrimiteConvocariAsync(sedintaId); // re-POST nu trebuie să dubleze

        var lista = await admin.GetFromJsonAsync<JsonElement>("/api/Dispozitii");
        var count = lista.EnumerateArray().Count(d =>
            d.GetProperty("sedintaId").ValueKind != JsonValueKind.Null
            && d.GetProperty("sedintaId").GetInt32() == sedintaId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Retry_SedintaFinalizata_Returneaza409()
    {
        using var admin = await AdminAsync();
        await admin.CreeazaConsilierAsync("Consilier Unu", "c1@test.ro");
        var sedintaId = await admin.CreeazaSedintaAsync();
        await admin.TrimiteConvocariAsync(sedintaId);
        var convocareId = (await admin.ListaConvocariAsync(sedintaId))[0].GetProperty("id").GetInt32();

        await admin.TranziteazaSedintaAsync(sedintaId, "Incepe");
        await admin.TranziteazaSedintaAsync(sedintaId, "Finalizeaza");

        var raspuns = await admin.PostAsync(
            $"/api/Sedinte/{sedintaId}/Convocari/{convocareId}/Retry", null);
        Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);
    }
}