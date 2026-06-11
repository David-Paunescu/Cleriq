using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Models;

namespace Cleriq.Tests.Infrastructura;

public record InstitutieDeTest(int Id, string Slug, string Denumire, string EmailAdmin, string ParolaAdmin);

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

    private static async Task AsigurareSucces(HttpResponseMessage raspuns, string operatiune)
    {
        if (raspuns.IsSuccessStatusCode) return;
        var detalii = await raspuns.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"{operatiune} eșuat: HTTP {(int)raspuns.StatusCode}. {detalii}");
    }
}