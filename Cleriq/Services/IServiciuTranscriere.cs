namespace Cleriq.Services;

public record RezultatTranscriere(
    bool Succes,
    bool EsteRetriable,
    string? ContinutJson,
    int? DurataAudioSecunde,
    string? Detalii);

public record RezultatVerificareTranscriere(
    bool Succes,
    int LatentaMs,
    string? Status,
    string? Device,
    string? ComputeType,
    string? Detalii);

public interface IServiciuTranscriere
{
    Task<RezultatTranscriere> TrimiteAsync(
        Stream audio,
        string numeFisier,
        ContinutTranscriere continut,
        CancellationToken ct = default);

    Task<RezultatVerificareTranscriere> VerificaAsync(CancellationToken ct = default);
}