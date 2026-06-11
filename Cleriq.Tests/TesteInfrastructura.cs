using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteInfrastructura
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteInfrastructura(CleriqFixture fixture)
    {
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task LoginSuperAdmin_CredentialeCorecte_ReturneazaToken()
    {
        var client = _factory.CreateClient();

        var raspuns = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = ConfigTest.SuperAdminEmail,
            parola = ConfigTest.SuperAdminParola
        });

        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);

        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        var token = corp.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task LoginSuperAdmin_ParolaGresita_Returneaza401()
    {
        var client = _factory.CreateClient();

        var raspuns = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = ConfigTest.SuperAdminEmail,
            parola = "parola-gresita"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, raspuns.StatusCode);
    }

    [Fact]
    public async Task PortalPublic_SlugInexistent_Returneaza404()
    {
        var client = _factory.CreateClient();

        var raspuns = await client.GetAsync("/public/slug-care-nu-exista");

        Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
    }
}