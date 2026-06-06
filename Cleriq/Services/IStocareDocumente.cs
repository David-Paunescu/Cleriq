namespace Cleriq.Services;

public record FisierStocat(string Cheie, long Marime, string HashSha256);

public record FisierFizicEnumerat(string Cheie, long Marime, DateTime DataModificare);

public interface IStocareDocumente
{
    Task<FisierStocat> SalveazaAsync(
        int institutieId,
        string numeFisierOriginal,
        Stream continut,
        CancellationToken ct = default);

    Task<Stream> DeschideAsync(string cheie, CancellationToken ct = default);

    Task StergeAsync(string cheie, CancellationToken ct = default);

    IAsyncEnumerable<FisierFizicEnumerat> EnumereazaToateAsync(CancellationToken ct = default);
}