using Twilio.Clients;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Cleriq.Services;

public class NotificareTwilio : IServiciuNotificareSms
{
    private readonly TwilioRestClient _client;
    private readonly string _fromNumber;
    private readonly ILogger<NotificareTwilio> _logger;

    public NotificareTwilio(IConfiguration config, ILogger<NotificareTwilio> logger)
    {
        var sid = config["Twilio:AccountSid"]
            ?? throw new InvalidOperationException("Twilio:AccountSid lipsește din config.");
        var token = config["Twilio:AuthToken"]
            ?? throw new InvalidOperationException("Twilio:AuthToken lipsește din config.");
        _fromNumber = config["Twilio:FromNumber"]
            ?? throw new InvalidOperationException("Twilio:FromNumber lipsește din config.");

        _client = new TwilioRestClient(sid, token);
        _logger = logger;
    }

    public async Task<RezultatTrimitere> TrimiteAsync(
        int institutieId, string telefonDestinatar, string continut, CancellationToken ct = default)
    {
        try
        {
            var mesaj = await MessageResource.CreateAsync(
                body: continut,
                from: new PhoneNumber(_fromNumber),
                to: new PhoneNumber(telefonDestinatar),
                client: _client);

            var eEsec = mesaj.Status == MessageResource.StatusEnum.Failed
                     || mesaj.Status == MessageResource.StatusEnum.Undelivered;

            var detalii = $"SID: {mesaj.Sid}, Status: {mesaj.Status}";
            return new RezultatTrimitere(!eEsec, detalii);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex,
                "Twilio API error la trimitere SMS (inst={Id}) → {Telefon}: {Code}",
                institutieId, telefonDestinatar, ex.Code);
            return new RezultatTrimitere(false, $"Twilio {ex.Code}: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Eroare neașteptată trimitere SMS (inst={Id}) → {Telefon}",
                institutieId, telefonDestinatar);
            return new RezultatTrimitere(false, $"{ex.GetType().Name}: {ex.Message}");
        }
    }
}