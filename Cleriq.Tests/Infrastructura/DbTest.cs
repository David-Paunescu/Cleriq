using Cleriq.Data;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Tests.Infrastructura;

// Acces direct la DB-ul de test, EXCLUSIV pentru aranjarea stărilor pe care
// doar worker-ii le produc. Verificările se fac întotdeauna prin API.
public static class DbTest
{
    public static AppDbContext CreeazaContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConfigTest.ConnectionStringDb)
            .Options;

        return new AppDbContext(options, new FurnizorTenantSystem(), new FurnizorUtilizatorNul());
    }

    public static async Task SeteazaStatusuriConvocareAsync(
        int convocareId,
        StatusTrimitere? emailStatus = null,
        StatusTrimitere? smsStatus = null)
    {
        await using var ctx = CreeazaContext();
        var convocare = await ctx.Convocari
            .IgnoreQueryFilters()
            .FirstAsync(c => c.Id == convocareId);

        var acum = DateTime.UtcNow;

        if (emailStatus.HasValue)
        {
            convocare.EmailStatus = emailStatus.Value;
            convocare.EmailTrimisLa = acum;
            convocare.EmailDetalii = "Aranjat direct în DB de test.";
        }

        if (smsStatus.HasValue)
        {
            convocare.SmsStatus = smsStatus.Value;
            convocare.SmsTrimisLa = acum;
            convocare.SmsDetalii = "Aranjat direct în DB de test.";
        }

        await ctx.SaveChangesAsync();
    }

    private sealed class FurnizorUtilizatorNul : IFurnizorUtilizator
    {
        public int? UserId => null;
    }
}