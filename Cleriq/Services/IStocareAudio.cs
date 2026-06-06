namespace Cleriq.Services;

public record FisierAudio(string Cheie, long Marime, string HashSha256);

public interface IStocareAudio
{
    Task<FisierAudio> SalveazaAsync(
        int institutieId,
        string numeFisierOriginal,
        Stream continut,
        CancellationToken ct = default);

    Task<Stream> DeschideAsync(string cheie, CancellationToken ct = default);

    Task StergeAsync(string cheie, CancellationToken ct = default);

    IAsyncEnumerable<FisierFizicEnumerat> EnumereazaToateAsync(CancellationToken ct = default);
}