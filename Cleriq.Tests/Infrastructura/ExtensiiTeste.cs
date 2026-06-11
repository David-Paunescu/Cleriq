using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

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
        this HttpClient clientAdmin, string numeComplet)
    {
        var raspuns = await clientAdmin.PostAsJsonAsync("/api/Consilieri", new
        {
            numeComplet,
            email = (string?)null,
            telefon = (string?)null
        });
        await AsigurareSucces(raspuns, "Creare consilier");

        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        return corp.GetProperty("id").GetInt32();
    }

    private static async Task AsigurareSucces(HttpResponseMessage raspuns, string operatiune)
    {
        if (raspuns.IsSuccessStatusCode) return;
        var detalii = await raspuns.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"{operatiune} eșuat: HTTP {(int)raspuns.StatusCode}. {detalii}");
    }
}