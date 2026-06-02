namespace Cleriq.Services;

public class NotificareLogger : IServiciuNotificare
{
    private readonly ILogger<NotificareLogger> _logger;

    public NotificareLogger(ILogger<NotificareLogger> logger)
    {
        _logger = logger;
    }

    public Task<IConexiuneEmail> DeschideConexiuneEmailAsync(
        int institutieId, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[NotificareLogger] Deschid conexiune EMAIL falsă pentru instituția {Id}.",
            institutieId);
        return Task.FromResult<IConexiuneEmail>(new ConexiuneEmailLogger(_logger, institutieId));
    }

    public Task<RezultatTrimitere> TrimiteSmsAsync(
        int institutieId, string telefonDestinatar, string continut, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[NotificareLogger] SMS (inst={Id}) → {Telefon} | Mesaj: \"{Mesaj}\"",
            institutieId, telefonDestinatar, continut);
        return Task.FromResult(new RezultatTrimitere(true, "Trimis prin logger (MVP)"));
    }

    private class ConexiuneEmailLogger : IConexiuneEmail
    {
        private readonly ILogger _logger;
        private readonly int _institutieId;

        public ConexiuneEmailLogger(ILogger logger, int institutieId)
        {
            _logger = logger;
            _institutieId = institutieId;
        }

        public Task<RezultatTrimitere> TrimiteAsync(
            string emailDestinatar, string subiect, string continutHtml, CancellationToken ct = default)
        {
            _logger.LogInformation(
                "[NotificareLogger] EMAIL (inst={Id}) → {Email} | Subiect: \"{Subiect}\" | HTML: {Lungime} caractere",
                _institutieId, emailDestinatar, subiect, continutHtml.Length);
            return Task.FromResult(new RezultatTrimitere(true, "Trimis prin logger (MVP)"));
        }

        public ValueTask DisposeAsync()
        {
            _logger.LogInformation("[NotificareLogger] Închid conexiunea EMAIL falsă (inst={Id}).", _institutieId);
            return ValueTask.CompletedTask;
        }
    }
}