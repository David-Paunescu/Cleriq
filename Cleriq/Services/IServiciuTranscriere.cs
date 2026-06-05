namespace Cleriq.Services;

public record RezultatTranscriere(
    bool Succes,
    bool EsteRetriable,
    string? ContinutJson,
    int? DurataAudioSecunde,
    string? Detalii);

public interface IServiciuTranscriere
{
    Task<RezultatTranscriere> TrimiteAsync(
        Stream audio,
        string numeFisier,
        string prompt,
        CancellationToken ct = default);
}