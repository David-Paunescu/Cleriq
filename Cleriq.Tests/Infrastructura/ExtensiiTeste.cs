using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;

namespace Cleriq.Tests.Infrastructura;

public record InstitutieDeTest(int Id, string Slug, string Denumire, string EmailAdmin, string ParolaAdmin);

public record HclAdoptat(
    int HclId, int SedintaId, int PunctId, int ConsilierId, int PersoanaSecretarId);

public static class ExtensiiTeste
{
    public static async Task<string> LoginAsync(
        this CleriqWebApplicationFactory factory, string email, string parola)
    {
        using var client = factory.CreateClient();
        var raspuns = await client.PostAsJsonAsync("/api/Auth/login", new { email, parola });
        await AsigurareSucces(raspuns, "Login");

        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        return corp.GetProperty("token").GetString()!;
    }

    public static async Task<HttpClient> ClientAutentificatAsync(
        this CleriqWebApplicationFactory factory, string email, string parola)
    {
        var token = await factory.LoginAsync(email, parola);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static Task<HttpClient> ClientSuperAdminAsync(this CleriqWebApplicationFactory factory)
        => factory.ClientAutentificatAsync(ConfigTest.SuperAdminEmail, ConfigTest.SuperAdminParola);

    public static async Task<InstitutieDeTest> ProvisioneazaInstitutieAsync(
        this CleriqWebApplicationFactory factory)
    {
        var sufix = Guid.NewGuid().ToString("N")[..8];
        var slug = $"primaria-test-{sufix}";
        var emailAdmin = $"admin.{sufix}@cleriq-test.ro";
        const string parolaAdmin = "AdminTest1!";

        using var superAdmin = await factory.ClientSuperAdminAsync();
        var raspuns = await superAdmin.PostAsJsonAsync("/api/Provisioning", new
        {
            denumire = $"Primăria Test {sufix}",
            judet = "Test",
            codSiruta = "000000",
            tip = 1,
            slug,
            emailAdmin,
            parolaAdmin,
            numeCompletAdmin = $"Admin Test {sufix}"
        });
        await AsigurareSucces(raspuns, "Provisioning");

        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        return new InstitutieDeTest(
            corp.GetProperty("institutieId").GetInt32(),
            slug,
            corp.GetProperty("denumire").GetString()!,
            emailAdmin,
            parolaAdmin);
    }

    public static async Task<int> CreeazaConsilierAsync(
            this HttpClient clientAdmin, string numeComplet, string? email = null, string? telefon = null)
    {
        var raspuns = await clientAdmin.PostAsJsonAsync("/api/Consilieri", new
        {
            numeComplet,
            email,
            telefon
        });
        await AsigurareSucces(raspuns, "Creare consilier");

        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        return corp.GetProperty("id").GetInt32();
    }

    public static async Task<int> CreeazaSedintaAsync(
        this HttpClient clientAdmin, string titlu = "Ședință Test")
    {
        var raspuns = await clientAdmin.PostAsJsonAsync("/api/Sedinte", new
        {
            titlu,
            numar = (string?)null,
            tip = TipSedinta.Ordinara,
            dataOra = DateTime.UtcNow.AddDays(7),
            loc = "Sala Test",
            modDesfasurare = ModDesfasurare.Fizic
        });
        await AsigurareSucces(raspuns, "Creare ședință");
        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        return corp.GetProperty("id").GetInt32();
    }

    public static async Task<JsonElement> TrimiteConvocariAsync(
        this HttpClient clientAdmin, int sedintaId)
    {
        var raspuns = await clientAdmin.PostAsync($"/api/Sedinte/{sedintaId}/Convocare", null);
        await AsigurareSucces(raspuns, "Trimitere convocări");
        return await raspuns.Content.ReadFromJsonAsync<JsonElement>();
    }

    public static async Task<JsonElement> ListaConvocariAsync(
        this HttpClient clientAdmin, int sedintaId)
        => await clientAdmin.GetFromJsonAsync<JsonElement>($"/api/Sedinte/{sedintaId}/Convocari");

    public static async Task TranziteazaSedintaAsync(
        this HttpClient clientAdmin, int sedintaId, string actiune)
    {
        var raspuns = await clientAdmin.PostAsync($"/api/Sedinte/{sedintaId}/{actiune}", null);
        await AsigurareSucces(raspuns, $"Tranziție ședință: {actiune}");
    }

    public static async Task<JsonElement> GenereazaProcesVerbalAsync(
        this HttpClient clientAdmin, int sedintaId)
    {
        var raspuns = await clientAdmin.PostAsync($"/api/Sedinte/{sedintaId}/ProcesVerbal/Genereaza", null);
        await AsigurareSucces(raspuns, "Generare proces verbal");
        return await raspuns.Content.ReadFromJsonAsync<JsonElement>();
    }

    public static async Task FinalizeazaProcesVerbalAsync(
        this HttpClient clientAdmin, int sedintaId)
    {
        var raspuns = await clientAdmin.PostAsync($"/api/Sedinte/{sedintaId}/ProcesVerbal/Finalizeaza", null);
        await AsigurareSucces(raspuns, "Finalizare proces verbal");
    }

    public static async Task<HttpResponseMessage> IncarcaPvSemnatAsync(
        this HttpClient clientAdmin, int sedintaId, byte[] continut, string numeFisier)
    {
        using var form = new MultipartFormDataContent();
        var parte = new ByteArrayContent(continut);
        parte.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(parte, "fisier", numeFisier);
        return await clientAdmin.PostAsync($"/api/Sedinte/{sedintaId}/ProcesVerbal/Semnat", form);
    }

    public static async Task<HttpResponseMessage> IncarcaAudioAsync(
        this HttpClient clientAdmin, int sedintaId, byte[] continut, string numeFisier)
    {
        using var form = new MultipartFormDataContent();
        var parte = new ByteArrayContent(continut);
        parte.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(parte, "fisier", numeFisier);
        return await clientAdmin.PostAsync($"/api/Sedinte/{sedintaId}/Transcriere", form);
    }

    public static async Task<JsonElement> IncarcaDocumentAsync(
        this HttpClient clientAdmin, byte[] continut, string numeFisier, string denumire,
        int? sedintaId = null, int? punctId = null)
    {
        using var form = new MultipartFormDataContent();
        var parte = new ByteArrayContent(continut);
        parte.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(parte, "fisier", numeFisier);
        form.Add(new StringContent(denumire), "denumire");
        form.Add(new StringContent(((int)TipDocument.Anexa).ToString()), "tipDocument");
        form.Add(new StringContent("0"), "ordine");
        if (sedintaId.HasValue) form.Add(new StringContent(sedintaId.Value.ToString()), "sedintaId");
        if (punctId.HasValue) form.Add(new StringContent(punctId.Value.ToString()), "punctId");

        var raspuns = await clientAdmin.PostAsync("/api/Documente", form);
        await AsigurareSucces(raspuns, "Încărcare document");
        return await raspuns.Content.ReadFromJsonAsync<JsonElement>();
    }

    public static async Task SeteazaVizibilitateDocumentAsync(
        this HttpClient clientAdmin, int documentId, bool estePublic)
    {
        var raspuns = await clientAdmin.PutAsJsonAsync(
            $"/api/Documente/{documentId}/Vizibilitate", new { estePublic });
        await AsigurareSucces(raspuns, "Setare vizibilitate document");
    }

    public static async Task SeteazaPrezentaAsync(
        this HttpClient clientAdmin, int sedintaId, int consilierId, StatusPrezenta status)
    {
        var raspuns = await clientAdmin.PostAsJsonAsync($"/api/Sedinte/{sedintaId}/Prezente",
            new { consilierId, status, oraSosire = (DateTime?)null });
        await AsigurareSucces(raspuns, "Setare prezență");
    }

    public static async Task<int> CreeazaPunctAsync(
        this HttpClient clientAdmin, int sedintaId, TipMajoritate tipMajoritate,
        int ordine = 1, TipVot? tipVot = null)
    {
        var raspuns = await clientAdmin.PostAsJsonAsync($"/api/Sedinte/{sedintaId}/Puncte", new
        {
            ordine,
            titlu = $"Punct {ordine}",
            descriere = (string?)null,
            tip = TipPunct.ProiectHCL,
            necesitaVot = true,
            tipMajoritate,
            tipVot
        });
        await AsigurareSucces(raspuns, "Creare punct");
        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        return corp.GetProperty("id").GetInt32();
    }

    public static async Task VoteazaAsync(
        this HttpClient clientAdmin, int sedintaId, int punctId, int consilierId, OptiuneVot optiune)
    {
        var raspuns = await clientAdmin.PostAsJsonAsync(
            $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Voturi",
            new { consilierId, optiune });
        await AsigurareSucces(raspuns, "Înregistrare vot");
    }

    public static async Task<JsonElement> InchideVotAsync(
        this HttpClient clientAdmin, int sedintaId, int punctId)
    {
        var raspuns = await clientAdmin.PostAsync(
            $"/api/Sedinte/{sedintaId}/Puncte/{punctId}/Inchide", null);
        await AsigurareSucces(raspuns, "Închidere vot");
        return await raspuns.Content.ReadFromJsonAsync<JsonElement>();
    }

    public static async Task<HttpClient> ClientConsilierAsync(
        this CleriqWebApplicationFactory factory, HttpClient clientAdmin, int consilierId)
    {
        var email = $"consilier.{Guid.NewGuid():N}@cleriq-test.ro";
        const string parola = "ConsilierTest1!";
        var raspuns = await clientAdmin.PostAsJsonAsync(
            $"/api/Consilieri/{consilierId}/Cont", new { email, parola });
        await AsigurareSucces(raspuns, "Creare cont consilier");
        return await factory.ClientAutentificatAsync(email, parola);
    }

    public static async Task<int> CreeazaPersoanaAsync(
    this HttpClient clientAdmin, string numeComplet,
    string? email = null, string? telefon = null)
    {
        var raspuns = await clientAdmin.PostAsJsonAsync("/api/Persoane", new
        {
            numeComplet,
            email,
            telefon
        });
        await AsigurareSucces(raspuns, "Creare persoană");
        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        return corp.GetProperty("id").GetInt32();
    }

    public static async Task<int> CreeazaMandatFunctieAsync(
        this HttpClient clientAdmin,
        TipFunctie tipFunctie,
        int? persoanaId,
        int? consilierId,
        DateOnly dataInceput,
        DateOnly? dataSfarsit = null,
        string? nrActNumire = null)
    {
        var raspuns = await clientAdmin.PostAsJsonAsync("/api/MandateFunctie", new
        {
            tipFunctie,
            persoanaId,
            consilierId,
            dataInceput,
            dataSfarsit,
            nrActNumire
        });
        await AsigurareSucces(raspuns, "Creare mandat funcție");
        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        return corp.GetProperty("id").GetInt32();
    }

    public static async Task InchideMandatFunctieAsync(
        this HttpClient clientAdmin, int mandatId, DateOnly dataSfarsit)
    {
        var raspuns = await clientAdmin.PostAsJsonAsync(
            $"/api/MandateFunctie/{mandatId}/Inchide",
            new { dataSfarsit });
        await AsigurareSucces(raspuns, "Închidere mandat funcție");
    }

    public static async Task<int> CreeazaComisieAsync(
        this HttpClient clientAdmin, string denumire, string? descriere = null)
    {
        var raspuns = await clientAdmin.PostAsJsonAsync("/api/Comisii", new { denumire, descriere });
        await AsigurareSucces(raspuns, "Creare comisie");
        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        return corp.GetProperty("id").GetInt32();
    }

    public static async Task AdaugaMembruComisieAsync(
        this HttpClient clientAdmin,
        int comisieId, int consilierId, RolComisie rol, DateOnly dataInceput)
    {
        var raspuns = await clientAdmin.PostAsJsonAsync(
            $"/api/Comisii/{comisieId}/Membri",
            new { consilierId, rol, dataInceput });
        await AsigurareSucces(raspuns, "Adăugare membru comisie");
    }

    public static async Task ScoateMembruComisieAsync(
        this HttpClient clientAdmin,
        int comisieId, int consilierId, DateOnly? dataSfarsit = null)
    {
        var url = $"/api/Comisii/{comisieId}/Membri/{consilierId}";
        if (dataSfarsit.HasValue)
            url += $"?dataSfarsit={dataSfarsit.Value:yyyy-MM-dd}";
        var raspuns = await clientAdmin.DeleteAsync(url);
        await AsigurareSucces(raspuns, "Scoatere membru comisie");
    }

    public static async Task ActualizeazaDataInceputMembruComisieAsync(
    this HttpClient clientAdmin,
    int comisieId, int consilierId, DateOnly dataInceput)
    {
        var raspuns = await clientAdmin.PutAsJsonAsync(
            $"/api/Comisii/{comisieId}/Membri/{consilierId}/DataInceput",
            new { dataInceput });
        await AsigurareSucces(raspuns, "Actualizare data început membru");
    }

    public static async Task<int> CreeazaMandatConsilierAsync(
        this HttpClient clientAdmin,
        int consilierId, DateOnly dataInceput,
        DateOnly? dataSfarsit = null, string? grupPolitic = null)
    {
        var raspuns = await clientAdmin.PostAsJsonAsync("/api/Mandate", new
        {
            consilierId,
            dataInceput,
            dataSfarsit,
            grupPolitic
        });
        await AsigurareSucces(raspuns, "Creare mandat consilier");
        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        return corp.GetProperty("id").GetInt32();
    }

    public static async Task ActualizeazaMandatConsilierAsync(
        this HttpClient clientAdmin, int mandatId,
        DateOnly dataInceput, DateOnly? dataSfarsit = null, string? grupPolitic = null)
    {
        var raspuns = await clientAdmin.PutAsJsonAsync($"/api/Mandate/{mandatId}", new
        {
            dataInceput,
            dataSfarsit,
            grupPolitic
        });
        await AsigurareSucces(raspuns, "Actualizare mandat consilier");
    }

    // === HCL (Modul A) ===

    // Lanțul complet până la un HCL Draft generat dintr-un punct adoptat: persoană +
    // mandat Secretar UAT valid la data adoptării, consilier-președinte, ședință,
    // prezență, punct ProiectHCL, vot Pentru, închidere (→ Adoptat), bump la Convocata.
    public static async Task<HclAdoptat> GenereazaHclAdoptatAsync(
        this HttpClient admin,
        TipHcl tipHcl = TipHcl.Normativ,
        TipMajoritate tipMajoritate = TipMajoritate.Simpla)
    {
        // Secretar UAT — un singur mandat per tenant (overlap e per-TipFunctie). La al 2-lea
        // HCL din același tenant refolosim secretarul existent, altfel mandatul ar da 409.
        var dataSedinta = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var secretarResp = await admin.GetAsync(
            $"/api/FunctiiIstorice/SecretarUat?data={dataSedinta:yyyy-MM-dd}");
        await AsigurareSucces(secretarResp, "Verificare Secretar UAT");
        var secretarJson = await secretarResp.Content.ReadFromJsonAsync<JsonElement>();

        int persoanaSecretarId;
        if (secretarJson.ValueKind == JsonValueKind.Null)
        {
            persoanaSecretarId = await admin.CreeazaPersoanaAsync("Secretar UAT Test");
            await admin.CreeazaMandatFunctieAsync(
                TipFunctie.SecretarUat, persoanaSecretarId, null,
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)));
        }
        else
        {
            persoanaSecretarId = secretarJson.GetProperty("id").GetInt32();
        }

        var consilierId = await admin.CreeazaConsilierAsync("Consilier Președinte");
        var sedintaId = await admin.CreeazaSedintaAsync();

        var raspunsPresedinte = await admin.PostAsJsonAsync(
            $"/api/Sedinte/{sedintaId}/PresedinteSedinta", new { consilierId });
        await AsigurareSucces(raspunsPresedinte, "Setare președinte ședință");

        await admin.SeteazaPrezentaAsync(sedintaId, consilierId, StatusPrezenta.Prezent);
        var punctId = await admin.CreeazaPunctAsync(sedintaId, tipMajoritate);
        await admin.VoteazaAsync(sedintaId, punctId, consilierId, OptiuneVot.Pentru);
        await admin.InchideVotAsync(sedintaId, punctId);

        // Genereaza cere Status >= Convocata; votul a mers pe Planificata (paritar PV).
        await DbTest.SeteazaStatusSedintaAsync(sedintaId, StatusSedinta.Convocata);

        var raspuns = await admin.PostAsJsonAsync(
            "/api/Hcl/Genereaza", new { punctOrdineZiId = punctId, tipHcl });
        await AsigurareSucces(raspuns, "Generare HCL");
        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();

        return new HclAdoptat(
            corp.GetProperty("id").GetInt32(),
            sedintaId, punctId, consilierId, persoanaSecretarId);
    }

    public static async Task<int> AtribuieNumarHclAsync(
        this HttpClient admin, int hclId, int numar = 1, bool confirmaCuLacune = false)
    {
        var raspuns = await admin.PostAsJsonAsync(
            $"/api/Hcl/{hclId}/AtribuieNumar", new { numar, confirmaCuLacune });
        await AsigurareSucces(raspuns, "Atribuire număr HCL");
        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        return corp.GetProperty("numar").GetInt32();
    }

    public static async Task SemneazaHclAsync(this HttpClient admin, int hclId)
    {
        var raspuns = await admin.PostAsync($"/api/Hcl/{hclId}/Semneaza", null);
        await AsigurareSucces(raspuns, "Semnare HCL");
    }

    public static async Task<HttpResponseMessage> IncarcaHclSemnatAsync(
        this HttpClient admin, int hclId, byte[] continut, string numeFisier)
    {
        using var form = new MultipartFormDataContent();
        var parte = new ByteArrayContent(continut);
        parte.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(parte, "fisier", numeFisier);
        return await admin.PostAsync($"/api/Hcl/{hclId}/Semnat", form);
    }

    // Anexă HCL: TipDocument trimis e Raport intenționat — backend-ul îl forțează la Altele.
    public static async Task<HttpResponseMessage> IncarcaAnexaHclAsync(
        this HttpClient admin, int hclId, byte[] continut, string numeFisier,
        string denumire, TipDocumentHcl tipDocumentHcl, int? numarOrdinAnexa)
    {
        using var form = new MultipartFormDataContent();
        var parte = new ByteArrayContent(continut);
        parte.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(parte, "fisier", numeFisier);
        form.Add(new StringContent(denumire), "denumire");
        form.Add(new StringContent(((int)TipDocument.Raport).ToString()), "tipDocument");
        form.Add(new StringContent("0"), "ordine");
        form.Add(new StringContent(hclId.ToString()), "hclId");
        form.Add(new StringContent(((int)tipDocumentHcl).ToString()), "tipDocumentHcl");
        if (numarOrdinAnexa.HasValue)
            form.Add(new StringContent(numarOrdinAnexa.Value.ToString()), "numarOrdinAnexa");
        return await admin.PostAsync("/api/Documente", form);
    }

    public static async Task<HttpClient> ClientSecretarAsync(
        this CleriqWebApplicationFactory factory, HttpClient clientAdmin)
    {
        var email = $"secretar.{Guid.NewGuid():N}@cleriq-test.ro";
        const string parola = "SecretarTest1!";
        var raspuns = await clientAdmin.PostAsJsonAsync("/api/Auth/register", new
        {
            email,
            parola,
            numeComplet = "Secretar Test",
            rol = "Secretar"
        });
        await AsigurareSucces(raspuns, "Creare cont secretar");
        return await factory.ClientAutentificatAsync(email, parola);
    }

    private static async Task AsigurareSucces(HttpResponseMessage raspuns, string operatiune)
    {
        if (raspuns.IsSuccessStatusCode) return;
        var detalii = await raspuns.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"{operatiune} eșuat: HTTP {(int)raspuns.StatusCode}. {detalii}");
    }
}