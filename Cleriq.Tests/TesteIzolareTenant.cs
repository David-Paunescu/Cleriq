using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteIzolareTenant
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteIzolareTenant(CleriqFixture fixture) => _factory = fixture.Factory;

    private async Task<(InstitutieDeTest InstA, HttpClient AdminA, InstitutieDeTest InstB, HttpClient AdminB)>
        DouaInstitutiiAsync()
    {
        var instA = await _factory.ProvisioneazaInstitutieAsync();
        var instB = await _factory.ProvisioneazaInstitutieAsync();
        var adminA = await _factory.ClientAutentificatAsync(instA.EmailAdmin, instA.ParolaAdmin);
        var adminB = await _factory.ClientAutentificatAsync(instB.EmailAdmin, instB.ParolaAdmin);
        return (instA, adminA, instB, adminB);
    }

    [Fact]
    public async Task ListaConsilieri_AdminulVedeDoarConsilieriiInstitutieiLui()
    {
        var (_, adminA, _, adminB) = await DouaInstitutiiAsync();
        using (adminA)
        using (adminB)
        {
            await adminA.CreeazaConsilierAsync("Consilier Al Lui A");
            await adminB.CreeazaConsilierAsync("Consilier Al Lui B");

            var listaA = await adminA.GetFromJsonAsync<JsonElement>("/api/Consilieri");

            var nume = listaA.EnumerateArray()
                .Select(c => c.GetProperty("numeComplet").GetString())
                .ToList();

            Assert.Contains("Consilier Al Lui A", nume);
            Assert.DoesNotContain("Consilier Al Lui B", nume);
        }
    }

    [Fact]
    public async Task DetaliiConsilier_IdDinAltTenant_Returneaza404()
    {
        var (_, adminA, _, adminB) = await DouaInstitutiiAsync();
        using (adminA)
        using (adminB)
        {
            var idConsilierB = await adminB.CreeazaConsilierAsync("Tinta Enumerare");

            var raspuns = await adminA.GetAsync($"/api/Consilieri/{idConsilierB}");

            Assert.Equal(HttpStatusCode.NotFound, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task StergereConsilier_IdDinAltTenant_Returneaza404SiNuSterge()
    {
        var (_, adminA, _, adminB) = await DouaInstitutiiAsync();
        using (adminA)
        using (adminB)
        {
            var idConsilierB = await adminB.CreeazaConsilierAsync("Tinta Stergere");

            var raspunsStergere = await adminA.DeleteAsync($"/api/Consilieri/{idConsilierB}");
            Assert.Equal(HttpStatusCode.NotFound, raspunsStergere.StatusCode);

            var verificare = await adminB.GetAsync($"/api/Consilieri/{idConsilierB}");
            Assert.Equal(HttpStatusCode.OK, verificare.StatusCode);
        }
    }

    [Fact]
    public async Task CreareConsilier_InstitutieIdSeForteazaDinToken()
    {
        var (instA, adminA, instB, _) = await DouaInstitutiiAsync();
        using (adminA)
        {
            var raspuns = await adminA.PostAsJsonAsync("/api/Consilieri", new
            {
                numeComplet = "Incercare Injectie Tenant",
                email = (string?)null,
                telefon = (string?)null,
                institutieId = instB.Id
            });

            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
            var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(instA.Id, corp.GetProperty("institutieId").GetInt32());
        }
    }

    [Fact]
    public async Task PortalPublic_SlugCorect_ReturneazaMetadateleInstitutiei()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        var client = _factory.CreateClient();

        var raspuns = await client.GetAsync($"/public/{inst.Slug}");

        Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
        var corp = await raspuns.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(inst.Denumire, corp.GetProperty("denumire").GetString());
    }

    [Fact]
    public async Task PortalPublic_SedintaAltuiTenantPrinSlugulMeu_Returneaza404()
    {
        var (instA, _, instB, adminB) = await DouaInstitutiiAsync();
        using (adminB)
        {
            await adminB.CreeazaConsilierAsync("Consilier B Convocare");

            var raspunsSedinta = await adminB.PostAsJsonAsync("/api/Sedinte", new
            {
                titlu = "Sedinta Lui B",
                numar = (string?)null,
                tip = 1,
                dataOra = DateTime.UtcNow.AddDays(7),
                loc = "Sala B",
                modDesfasurare = 1
            });
            var sedintaB = await raspunsSedinta.Content.ReadFromJsonAsync<JsonElement>();
            var idSedintaB = sedintaB.GetProperty("id").GetInt32();

            var raspunsConvocare = await adminB.PostAsync($"/api/Sedinte/{idSedintaB}/Convocare", null);
            Assert.Equal(HttpStatusCode.OK, raspunsConvocare.StatusCode);

            var client = _factory.CreateClient();

            var prinSlugB = await client.GetAsync($"/public/{instB.Slug}/sedinte/{idSedintaB}");
            Assert.Equal(HttpStatusCode.OK, prinSlugB.StatusCode);

            var prinSlugA = await client.GetAsync($"/public/{instA.Slug}/sedinte/{idSedintaB}");
            Assert.Equal(HttpStatusCode.NotFound, prinSlugA.StatusCode);
        }
    }
}