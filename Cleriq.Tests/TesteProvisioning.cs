using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteProvisioning
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteProvisioning(CleriqFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task Provisioning_CreeazaInstitutie_AdminulSePoateAutentifica()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();

        using var admin = await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);
        var raspuns = await admin.GetAsync("/api/Consilieri");

        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
    }

    [Fact]
    public async Task Provisioning_SlugDuplicat_Returneaza409CuSugestii()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();

        using var superAdmin = await _factory.ClientSuperAdminAsync();
        var raspuns = await superAdmin.PostAsJsonAsync("/api/Provisioning", new
        {
            denumire = "Duplicat",
            judet = "Test",
            codSiruta = "000000",
            tip = 1,
            slug = inst.Slug,
            emailAdmin = $"alt.admin.{Guid.NewGuid():N}@cleriq-test.ro",
            parolaAdmin = "AdminTest1!",
            numeCompletAdmin = "Alt Admin"
        });

        Assert.Equal(HttpStatusCode.Conflict, raspuns.StatusCode);

        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(corp.GetProperty("sugestii").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Provisioning_FaraToken_Returneaza401()
    {
        var client = _factory.CreateClient();

        var raspuns = await client.PostAsJsonAsync("/api/Provisioning", new { denumire = "X" });

        Assert.Equal(HttpStatusCode.Unauthorized, raspuns.StatusCode);
    }

    [Fact]
    public async Task Provisioning_CuTokenAdmin_Returneaza403()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        using var admin = await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);

        var raspuns = await admin.PostAsJsonAsync("/api/Provisioning", new { denumire = "X" });

        Assert.Equal(HttpStatusCode.Forbidden, raspuns.StatusCode);
    }
}