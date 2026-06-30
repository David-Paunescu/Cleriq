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

    // Semnarea dispoziției e Pas 6; până atunci forțăm statusul direct ca să testăm gărzile
    // care se activează pe Semnat (editare/regenerare conținut blocate).
    public static async Task SeteazaStatusDispozitieAsync(int dispozitieId, StatusActRedactional status)
    {
        await using var ctx = CreeazaContext();
        var d = await ctx.Dispozitii.IgnoreQueryFilters().FirstAsync(x => x.Id == dispozitieId);
        d.Status = status;
        await ctx.SaveChangesAsync();
    }

    // Delete-ul de dispoziție e Pas 8; soft-ștergem direct ca să testăm „numărul ars" la numerotare
    // (un act retras își păstrează numărul ars în registru). Oglindește soft-delete-ul real.
    public static async Task SoftDeleteDispozitieAsync(int dispozitieId)
    {
        await using var ctx = CreeazaContext();
        var d = await ctx.Dispozitii.IgnoreQueryFilters().FirstAsync(x => x.Id == dispozitieId);
        d.EsteSters = true;
        d.StersLa = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    // Latch-ul „intrat în circuit" se aprinde la publicare/comunicare (Pas 9/10); până atunci îl
    // forțăm direct ca să testăm înghețarea variantei semnate (replace/delete blocate) + regula de revocare.
    public static async Task SeteazaAIntratInCircuitDispozitieAsync(int dispozitieId, bool valoare = true)
    {
        await using var ctx = CreeazaContext();
        var d = await ctx.Dispozitii.IgnoreQueryFilters().FirstAsync(x => x.Id == dispozitieId);
        d.AIntratInCircuit = valoare;
        await ctx.SaveChangesAsync();
    }

    // Publicarea e Pas 9; forțăm flag-ul direct ca să testăm garda de DELETE pe dispoziție publicată.
    public static async Task SeteazaEstePublicatDispozitieAsync(int dispozitieId, bool valoare = true)
    {
        await using var ctx = CreeazaContext();
        var d = await ctx.Dispozitii.IgnoreQueryFilters().FirstAsync(x => x.Id == dispozitieId);
        d.EstePublicat = valoare;
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

    public static async Task<string?> CitesteCaleStocareSemnatHclAsync(int hclId)
    {
        await using var ctx = CreeazaContext();
        return await ctx.Hcluri
            .IgnoreQueryFilters()
            .Where(h => h.Id == hclId)
            .Select(h => h.CaleStocareSemnat)
            .FirstOrDefaultAsync();
    }

    public static async Task<string?> CitesteCaleStocareSemnatDispozitieAsync(int dispozitieId)
    {
        await using var ctx = CreeazaContext();
        return await ctx.Dispozitii
            .IgnoreQueryFilters()
            .Where(d => d.Id == dispozitieId)
            .Select(d => d.CaleStocareSemnat)
            .FirstOrDefaultAsync();
    }

    // Data adoptării vine din ședință (UtcNow+7); pentru testele de termene T-N o forțăm în trecut.
    public static async Task SeteazaDataAdoptareHclAsync(int hclId, DateTime dataAdoptare)
    {
        await using var ctx = CreeazaContext();
        var hcl = await ctx.Hcluri.IgnoreQueryFilters().FirstAsync(h => h.Id == hclId);
        hcl.DataAdoptare = dataAdoptare;
        await ctx.SaveChangesAsync();
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