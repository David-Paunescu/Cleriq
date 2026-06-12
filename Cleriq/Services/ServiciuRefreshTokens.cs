using System.Security.Cryptography;
using System.Text;
using Cleriq.Data;
using Cleriq.Models;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Services;

public class ServiciuRefreshTokens : IServiciuRefreshTokens
{
    private static readonly TimeSpan FereastraGratie = TimeSpan.FromSeconds(60);

    private readonly AppDbContext _context;
    private readonly ILogger<ServiciuRefreshTokens> _logger;
    private readonly int _expirareZile;

    public ServiciuRefreshTokens(
        AppDbContext context, IConfiguration config, ILogger<ServiciuRefreshTokens> logger)
    {
        _context = context;
        _logger = logger;
        _expirareZile = Math.Max(1, config.GetValue<int>("Jwt:RefreshExpirareZile", 60));
    }

    public async Task<string> EmiteLaLoginAsync(int utilizatorId, CancellationToken ct = default)
    {
        var acum = DateTime.UtcNow;

        // Cleanup amortizat: tokenurile moarte ale utilizatorului se șterg fizic la login.
        await _context.RefreshTokens
            .Where(rt => rt.UtilizatorId == utilizatorId
                && (rt.ExpiraLa <= acum || rt.RevocatLa != null))
            .ExecuteDeleteAsync(ct);

        return await CreeazaAsync(utilizatorId, Guid.NewGuid(), acum, ct);
    }

    public async Task<RezultatRefresh> ValideazaSiRotesteAsync(
        string refreshToken, CancellationToken ct = default)
    {
        var esec = new RezultatRefresh(false, 0, null);
        if (string.IsNullOrWhiteSpace(refreshToken)) return esec;

        var hash = Hash(refreshToken);
        var acum = DateTime.UtcNow;

        // Claim atomic: o singură cerere poate consuma tokenul (UPDATE condiționat în DB).
        var revendicat = await _context.RefreshTokens
            .Where(rt => rt.TokenHash == hash
                && rt.FolositLa == null
                && rt.RevocatLa == null
                && rt.ExpiraLa > acum)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.FolositLa, acum), ct);

        if (revendicat == 1)
        {
            var vechi = await _context.RefreshTokens.AsNoTracking()
                .FirstAsync(rt => rt.TokenHash == hash, ct);
            var tokenNou = await CreeazaAsync(vechi.UtilizatorId, vechi.Familie, acum, ct);
            return new RezultatRefresh(true, vechi.UtilizatorId, tokenNou);
        }

        var existent = await _context.RefreshTokens.AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);

        if (existent is null || existent.RevocatLa != null || existent.ExpiraLa <= acum)
            return esec;

        // FolositLa setat = refolosire. În grație = cursă legitimă între taburi (doar 401);
        // după grație = semnal de furt → revocăm întreaga familie.
        if (existent.FolositLa is not null && acum - existent.FolositLa.Value > FereastraGratie)
        {
            _logger.LogWarning(
                "Refolosire refresh token în afara grației (utilizator {UserId}, familie {Familie}). Revoc familia.",
                existent.UtilizatorId, existent.Familie);
            await RevocaFamilieAsync(existent.Familie, acum, ct);
        }

        return esec;
    }

    public async Task RevocaAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var existent = await _context.RefreshTokens.AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.TokenHash == Hash(refreshToken), ct);
        if (existent is null) return;

        await RevocaFamilieAsync(existent.Familie, DateTime.UtcNow, ct);
    }

    public Task RevocaToateAsync(int utilizatorId, CancellationToken ct = default)
        => _context.RefreshTokens
            .Where(rt => rt.UtilizatorId == utilizatorId && rt.RevocatLa == null)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevocatLa, DateTime.UtcNow), ct);

    private Task RevocaFamilieAsync(Guid familie, DateTime acum, CancellationToken ct)
        => _context.RefreshTokens
            .Where(rt => rt.Familie == familie && rt.RevocatLa == null)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevocatLa, acum), ct);

    private async Task<string> CreeazaAsync(
        int utilizatorId, Guid familie, DateTime acum, CancellationToken ct)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        _context.RefreshTokens.Add(new RefreshToken
        {
            UtilizatorId = utilizatorId,
            Familie = familie,
            TokenHash = Hash(token),
            CreatLa = acum,
            ExpiraLa = acum.AddDays(_expirareZile)
        });
        await _context.SaveChangesAsync(ct);

        return token;
    }

    private static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}