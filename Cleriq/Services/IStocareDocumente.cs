namespace Cleriq.Services;

public record FisierStocat(string Cheie, long Marime, string HashSha256);

public interface IStocareDocumente
{
    Task<FisierStocat> SalveazaAsync(
        int institutieId,
        string numeFisierOriginal,
        Stream continut,
        CancellationToken ct = default);

    Task<Stream> DeschideAsync(string cheie, CancellationToken ct = default);

    Task StergeAsync(string cheie, CancellationToken ct = default);
}