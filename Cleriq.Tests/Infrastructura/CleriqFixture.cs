using Cleriq.Data;
using Cleriq.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Cleriq.Tests.Infrastructura;

// Fixture de colecție: O instanță de aplicație + UN reset de DB per rulare.
// REGULĂ: orice clasă de teste de integrare se atașează cu [Collection("Cleriq")]
// și primește fixture-ul în constructor — niciodată factory propriu.
public class CleriqFixture : IAsyncLifetime
{
    public CleriqWebApplicationFactory Factory { get; } = new();

    public async Task InitializeAsync()
    {
        await ReseteazaBazaDeDateAsync();
        await GolesteRedisDeTestAsync();
        StergeStocareaDeTest();

        // Abia ACUM pornim host-ul (primul acces la Services îl construiește):
        // seed-ul din Program.cs (roluri + SuperAdmin) are nevoie de DB-ul deja migrat.
        _ = Factory.Services;
    }

    public async Task DisposeAsync() => await Factory.DisposeAsync();

    private static async Task ReseteazaBazaDeDateAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConfigTest.ConnectionStringDb)
            .Options;

        await using var db = new AppDbContext(
            options, new FurnizorTenantSystem(), new FurnizorUtilizatorSystem());

        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    // Stocarea pe disk se resetează ca DB-ul: fiecare rulare pornește ermetic,
    // altfel fișierele rulărilor anterioare ar apărea ca orfani FaraRandInDb.
    private static void StergeStocareaDeTest()
    {
        foreach (var root in new[] { ConfigTest.CaleRootDocumente, ConfigTest.CaleRootAudio })
        {
            var cale = Path.Combine(AppContext.BaseDirectory, root);
            if (Directory.Exists(cale))
                Directory.Delete(cale, recursive: true);
        }
    }

    // Golește EXCLUSIV indexul 15 (test). NICIODATĂ index 0 sau FlushAll —
    // pe index 0 stau cheile Data Protection ale mediului de dev (parolele SMTP)!
    private static async Task GolesteRedisDeTestAsync()
    {
        var conexiune = await ConnectionMultiplexer.ConnectAsync(ConfigTest.ConnectionStringRedisAdmin);
        try
        {
            foreach (var endpoint in conexiune.GetEndPoints())
                await conexiune.GetServer(endpoint).FlushDatabaseAsync(ConfigTest.RedisDatabaseIndex);
        }
        finally
        {
            conexiune.Dispose();
        }
    }

    // AppDbContext cere IFurnizorUtilizator; la operațiuni de infrastructură nu există user.
    private sealed class FurnizorUtilizatorSystem : IFurnizorUtilizator
    {
        public int? UserId => null;
    }
}

[CollectionDefinition("Cleriq")]
public class ColectiaCleriq : ICollectionFixture<CleriqFixture>
{
}