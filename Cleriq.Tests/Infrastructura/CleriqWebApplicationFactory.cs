using Cleriq.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cleriq.Tests.Infrastructura;

public class CleriqWebApplicationFactory : WebApplicationFactory<Program>
{
    public CleriqWebApplicationFactory()
    {
        ClientOptions.BaseAddress = new Uri("https://localhost");

        SeteazaVariabileDeMediu();
    }

    private static void SeteazaVariabileDeMediu()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", ConfigTest.ConnectionStringDb);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", ConfigTest.ConnectionStringRedis);
        Environment.SetEnvironmentVariable("Jwt__Key", ConfigTest.JwtKey);
        Environment.SetEnvironmentVariable("SuperAdmin__Email", ConfigTest.SuperAdminEmail);
        Environment.SetEnvironmentVariable("SuperAdmin__Parola", ConfigTest.SuperAdminParola);

        Environment.SetEnvironmentVariable("DirectorDocumente__CaleRoot", ConfigTest.CaleRootDocumente);
        Environment.SetEnvironmentVariable("DirectorAudio__CaleRoot", ConfigTest.CaleRootAudio);

        Environment.SetEnvironmentVariable("Whisper__UrlBaza", null);
        Environment.SetEnvironmentVariable("Twilio__AccountSid", null);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            var workeri = services
                .Where(d => d.ServiceType == typeof(IHostedService)
                         && (d.ImplementationType == typeof(WorkerConvocari)
                          || d.ImplementationType == typeof(WorkerTranscrieri)))
                .ToList();

            foreach (var descriptor in workeri)
                services.Remove(descriptor);
        });
    }
}