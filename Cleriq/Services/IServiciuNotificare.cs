namespace Cleriq.Services;

public record RezultatTrimitere(bool Succes, string? Detalii);

public interface IServiciuNotificare
{
    Task<RezultatTrimitere> TrimiteEmailAsync(
        string emailDestinatar,
        string subiect,
        string continutHtml,
        CancellationToken ct = default);

    Task<RezultatTrimitere> TrimiteSmsAsync(
        string telefonDestinatar,
        string continut,
        CancellationToken ct = default);
}