using System.Net;
using Cleriq.Tests.Infrastructura;

namespace Cleriq.Tests;

[Collection("Cleriq")]
public class TesteRateLimiting
{
    private readonly CleriqWebApplicationFactory _factory;

    public TesteRateLimiting(CleriqFixture fixture) => _factory = fixture.Factory;

    private static Task AsteaptaFereastraNouaAsync()
        => Task.Delay(TimeSpan.FromSeconds(ConfigTest.RateLimitPublicFereastraSecunde + 1));

    [Fact]
    public async Task RutePublice_PesteLimita_429CuRetryAfter_ApoiFereastraNouaPermite()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        var client = _factory.CreateClient();
        var url = $"/public/{inst.Slug}";

        await AsteaptaFereastraNouaAsync();

        HttpResponseMessage? respins = null;
        for (var i = 0; i < ConfigTest.RateLimitPublicRequesturi + 5; i++)
        {
            var raspuns = await client.GetAsync(url);
            if (raspuns.StatusCode == HttpStatusCode.TooManyRequests)
            {
                respins = raspuns;
                break;
            }
            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
        }

        Assert.NotNull(respins);
        Assert.True(respins.Headers.Contains("Retry-After"));

        await AsteaptaFereastraNouaAsync();
        var dupaFereastra = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, dupaFereastra.StatusCode);
    }

    [Fact]
    public async Task RuteInterne_NuSuntLimitate()
    {
        var inst = await _factory.ProvisioneazaInstitutieAsync();
        using var admin = await _factory.ClientAutentificatAsync(inst.EmailAdmin, inst.ParolaAdmin);

        for (var i = 0; i < ConfigTest.RateLimitPublicRequesturi + 5; i++)
        {
            var raspuns = await admin.GetAsync("/api/Consilieri");
            Assert.Equal(HttpStatusCode.OK, raspuns.StatusCode);
        }
    }

    [Fact]
    public async Task RutaPublicaRespinsa_NuAjungeLaRezolvareaSlugului()
    {
        var client = _factory.CreateClient();

        await AsteaptaFereastraNouaAsync();

        for (var i = 0; i < ConfigTest.RateLimitPublicRequesturi; i++)
            await client.GetAsync("/public/slug-inexistent-flood");

        var pesteLimita = await client.GetAsync("/public/slug-inexistent-flood");
        Assert.Equal(HttpStatusCode.TooManyRequests, pesteLimita.StatusCode);

        await AsteaptaFereastraNouaAsync();
    }
}