namespace Cleriq.Services;

public record RezultatTrimitere(bool Succes, string? Detalii);

public interface IConexiuneEmail : IAsyncDisposable
{
    Task<RezultatTrimitere> TrimiteAsync(
        string emailDestinatar,
        string subiect,
        string continutHtml,
        CancellationToken ct = default);
}

public interface IServiciuNotificare
{
    Task<IConexiuneEmail> DeschideConexiuneEmailAsync(
        int institutieId,
        CancellationToken ct = default);

    Task<RezultatTrimitere> TrimiteSmsAsync(
        int institutieId,
        string telefonDestinatar,
        string continut,
        CancellationToken ct = default);
}