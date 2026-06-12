namespace Cleriq.Services;

public record RezultatRefresh(bool Succes, int UtilizatorId, string? TokenNou);

public interface IServiciuRefreshTokens
{
    Task<string> EmiteLaLoginAsync(int utilizatorId, CancellationToken ct = default);
    Task<RezultatRefresh> ValideazaSiRotesteAsync(string refreshToken, CancellationToken ct = default);
    Task RevocaAsync(string refreshToken, CancellationToken ct = default);
    Task RevocaToateAsync(int utilizatorId, CancellationToken ct = default);
}