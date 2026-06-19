using Cleriq.Data;
using Cleriq.Models;
using Cleriq.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

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

    public static async Task SeteazaStatusTranscriereAsync(
        int transcriereId,
        StatusTranscriere status,
        string? continutBrut = null)
    {
        await using var ctx = CreeazaContext();
        var transcriere = await ctx.Transcrieri
            .IgnoreQueryFilters()
            .FirstAsync(t => t.Id == transcriereId);

        transcriere.Status = status;

        if (status == StatusTranscriere.Esuata)
        {
            transcriere.NumarEsecuri = 3;
            transcriere.UltimaEroare = "Aranjat direct în DB de test.";
            transcriere.UrmatoareaIncercareDupa = null;
        }
        else if (status == StatusTranscriere.Finalizata)
        {
            transcriere.ContinutBrut = continutBrut ?? """{"text": []}""";
            transcriere.DataPrimireBrut = DateTime.UtcNow;
            transcriere.UltimaEroare = null;
        }

        await ctx.SaveChangesAsync();
    }

    public static async Task SeteazaStatusSedintaAsync(int sedintaId, StatusSedinta status)
    {
        await using var ctx = CreeazaContext();
        var s = await ctx.Sedinte.IgnoreQueryFilters().FirstAsync(x => x.Id == sedintaId);
        s.Status = status;
        await ctx.SaveChangesAsync();
    }

    public static async Task<string?> CitesteCaleStocareSemnatAsync(int sedintaId)
    {
        await using var ctx = CreeazaContext();
        return await ctx.ProceseVerbale
            .IgnoreQueryFilters()
            .Where(p => p.SedintaId == sedintaId)
            .Select(p => p.CaleStocareSemnat)
            .FirstOrDefaultAsync();
    }

    public static async Task<string> CitesteCaleStocareDocumentAsync(int documentId)
    {
        await using var ctx = CreeazaContext();
        return await ctx.Documente
            .IgnoreQueryFilters()
            .Where(d => d.Id == documentId)
            .Select(d => d.CaleStocare)
            .FirstAsync();
    }

    public static async Task<string> CitesteCaleStocareAudioAsync(int sedintaId)
    {
        await using var ctx = CreeazaContext();
        return await ctx.Transcrieri
            .IgnoreQueryFilters()
            .Where(t => t.SedintaId == sedintaId)
            .Select(t => t.CaleStocareAudio)
            .FirstAsync();
    }

    public static async Task SeteazaStersLaDocumentAsync(int documentId, DateTime stersLa)
    {
        await using var ctx = CreeazaContext();
        var doc = await ctx.Documente.IgnoreQueryFilters().FirstAsync(d => d.Id == documentId);
        doc.StersLa = stersLa;
        await ctx.SaveChangesAsync();
    }

    public static async Task SeteazaStersLaTranscriereAsync(int transcriereId, DateTime stersLa)
    {
        await using var ctx = CreeazaContext();
        var transcriere = await ctx.Transcrieri.IgnoreQueryFilters().FirstAsync(t => t.Id == transcriereId);
        transcriere.StersLa = stersLa;
        await ctx.SaveChangesAsync();
    }

    public static async Task SeteazaRefreshTokenFolositLaAsync(string refreshToken, DateTime folositLa)
    {
        await using var ctx = CreeazaContext();
        var rt = await ctx.RefreshTokens
            .FirstAsync(r => r.TokenHash == HashRefreshToken(refreshToken));
        rt.FolositLa = folositLa;
        await ctx.SaveChangesAsync();
    }

    public static async Task SeteazaRefreshTokenExpiraLaAsync(string refreshToken, DateTime expiraLa)
    {
        await using var ctx = CreeazaContext();
        var rt = await ctx.RefreshTokens
            .FirstAsync(r => r.TokenHash == HashRefreshToken(refreshToken));
        rt.ExpiraLa = expiraLa;
        await ctx.SaveChangesAsync();
    }

    // Oglindește hash-ul din ServiciuRefreshTokens — pin de contract: o schimbare
    // de algoritm ar invalida toate tokenurile emise deja în producție.
    private static string HashRefreshToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private sealed class FurnizorUtilizatorNul : IFurnizorUtilizator
    {
        public int? UserId => null;
    }
}