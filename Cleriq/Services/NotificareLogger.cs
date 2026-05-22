using Microsoft.Extensions.Logging;

namespace Cleriq.Services;

public class NotificareLogger : IServiciuNotificare
{
    private readonly ILogger<NotificareLogger> _logger;

    public NotificareLogger(ILogger<NotificareLogger> logger)
    {
        _logger = logger;
    }

    public Task<RezultatTrimitere> TrimiteEmailAsync(
        string emailDestinatar, string subiect, string continutHtml, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[NotificareLogger] EMAIL → {Email} | Subiect: \"{Subiect}\" | HTML: {Lungime} caractere",
            emailDestinatar, subiect, continutHtml.Length);
        return Task.FromResult(new RezultatTrimitere(true, "Trimis prin logger (MVP)"));
    }

    public Task<RezultatTrimitere> TrimiteSmsAsync(
        string telefonDestinatar, string continut, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[NotificareLogger] SMS → {Telefon} | Mesaj: \"{Mesaj}\"",
            telefonDestinatar, continut);
        return Task.FromResult(new RezultatTrimitere(true, "Trimis prin logger (MVP)"));
    }
}