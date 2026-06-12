using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteRefreshTokens
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteRefreshTokens(CleriqFixture fixture) => _factory = fixture.Factory;

    private static async Task<(string Token, string RefreshToken)> LoginSuperAdminAsync(HttpClient client)
    {
        var raspuns = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = ConfigTest.SuperAdminEmail,
            parola = ConfigTest.SuperAdminParola
        });
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        return (corp.GetProperty("token").GetString()!,
                corp.GetProperty("refreshToken").GetString()!);
    }

    private static Task<HttpResponseMessage> RefreshAsync(HttpClient client, string refreshToken)
        => client.PostAsJsonAsync("/api/Auth/refresh", new { refreshToken });

    private static Task<HttpResponseMessage> LogoutAsync(HttpClient client, string refreshToken)
        => client.PostAsJsonAsync("/api/Auth/logout", new { refreshToken });

    private static async Task<string> RefreshTokenDin(HttpResponseMessage raspuns)
        => (await raspuns.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("refreshToken").GetString()!;

    [Fact]
    public async Task Login_ReturneazaTokenSiRefreshToken()
    {
        var client = _factory.CreateClient();
        var (token, refreshToken) = await LoginSuperAdminAsync(client);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.False(string.IsNullOrWhiteSpace(refreshToken));
    }

    [Fact]
    public async Task Refresh_TokenValid_RotesteSiEmiteJwtFunctional()
    {
        var client = _factory.CreateClient();
        var (_, refreshVechi) = await LoginSuperAdminAsync(client);

        var raspuns = await RefreshAsync(client, refreshVechi);
        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        var tokenNou = corp.GetProperty("token").GetString()!;
        var refreshNou = corp.GetProperty("refreshToken").GetString()!;
        Assert.NotEqual(refreshVechi, refreshNou);

        using var autentificat = _factory.CreateClient();
        autentificat.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenNou);
        var protejat = await autentificat.GetAsync("/api/Institutii");
        Assert.Equal(HttpStatusCode.OK, protejat.StatusCode);
    }

    [Fact]
    public async Task Refresh_RefolosireInGratie_401_DarFamiliaSupravietuieste()
    {
        var client = _factory.CreateClient();
        var (_, refreshVechi) = await LoginSuperAdminAsync(client);

        var primul = await RefreshAsync(client, refreshVechi);
        Assert.Equal(HttpStatusCode.OK, primul.StatusCode);
        var refreshNou = await RefreshTokenDin(primul);

        var refolosire = await RefreshAsync(client, refreshVechi);
        Assert.Equal(HttpStatusCode.Unauthorized, refolosire.StatusCode);

        var curent = await RefreshAsync(client, refreshNou);
        Assert.Equal(HttpStatusCode.OK, curent.StatusCode);
    }

    [Fact]
    public async Task Refresh_RefolosireDupaGratie_RevocaIntreagaFamilie()
    {
        var client = _factory.CreateClient();
        var (_, refreshVechi) = await LoginSuperAdminAsync(client);

        var primul = await RefreshAsync(client, refreshVechi);
        Assert.Equal(HttpStatusCode.OK, primul.StatusCode);
        var refreshNou = await RefreshTokenDin(primul);

        await DbTest.SeteazaRefreshTokenFolositLaAsync(refreshVechi, DateTime.UtcNow.AddMinutes(-5));

        var refolosire = await RefreshAsync(client, refreshVechi);
        Assert.Equal(HttpStatusCode.Unauthorized, refolosire.StatusCode);

        var curent = await RefreshAsync(client, refreshNou);
        Assert.Equal(HttpStatusCode.Unauthorized, curent.StatusCode);
    }

    [Fact]
    public async Task Refresh_TokenExpirat_401()
    {
        var client = _factory.CreateClient();
        var (_, refresh) = await LoginSuperAdminAsync(client);

        await DbTest.SeteazaRefreshTokenExpiraLaAsync(refresh, DateTime.UtcNow.AddMinutes(-1));

        var raspuns = await RefreshAsync(client, refresh);
        Assert.Equal(HttpStatusCode.Unauthorized, raspuns.StatusCode);
    }

    [Fact]
    public async Task Refresh_TokenInexistent_401()
    {
        var client = _factory.CreateClient();

        var raspuns = await RefreshAsync(client, "token-care-nu-exista");
        Assert.Equal(HttpStatusCode.Unauthorized, raspuns.StatusCode);
    }

    [Fact]
    public async Task Logout_RevocaFamilia_SiEsteIdempotent()
    {
        var client = _factory.CreateClient();
        var (_, refreshVechi) = await LoginSuperAdminAsync(client);
        var primul = await RefreshAsync(client, refreshVechi);
        var refreshCurent = await RefreshTokenDin(primul);

        Assert.Equal(HttpStatusCode.NoContent,
            (await LogoutAsync(client, refreshCurent)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await RefreshAsync(client, refreshCurent)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await LogoutAsync(client, refreshCurent)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await LogoutAsync(client, "token-necunoscut")).StatusCode);
    }
}