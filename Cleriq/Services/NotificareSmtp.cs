using Cleriq.Data;
using Cleriq.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Cleriq.Services;

public class NotificareSmtp : IServiciuNotificareEmail
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICriptareSecreta _criptare;
    private readonly ILogger<NotificareSmtp> _logger;
    private readonly int _timeoutSecunde;

    public NotificareSmtp(
        IServiceScopeFactory scopeFactory,
        ICriptareSecreta criptare,
        ILogger<NotificareSmtp> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _criptare = criptare;
        _logger = logger;
        _timeoutSecunde = Math.Max(5, config.GetValue<int>("Smtp:TimeoutSecunde", 30));
    }

    public async Task<IConexiuneEmail> DeschideConexiuneEmailAsync(
        int institutieId, CancellationToken ct = default)
    {
        var config = await CitesteConfigAsync(institutieId, ct);

        var client = new SmtpClient
        {
            Timeout = _timeoutSecunde * 1000
        };

        try
        {
            var optiuni = RezolvaOptiuniSecuritate(config.Securitate, config.Port);
            await client.ConnectAsync(config.Host, config.Port, optiuni, ct);
            await client.AuthenticateAsync(config.Utilizator, config.Parola, ct);
        }
        catch
        {
            client.Dispose();
            throw;
        }

        return new ConexiuneSmtp(client, config.EmailFrom, config.NumeFrom, _logger);
    }

    private async Task<ConfigSmtpDecriptat> CitesteConfigAsync(int institutieId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var options = sp.GetRequiredService<DbContextOptions<AppDbContext>>();
        var furnizorUtilizator = sp.GetRequiredService<IFurnizorUtilizator>();

        using var ctx = new AppDbContext(options, new FurnizorTenantSystem(), furnizorUtilizator);

        var inst = await ctx.Institutii
            .IgnoreQueryFilters()
            .Where(i => i.Id == institutieId && !i.EsteSters)
            .Select(i => new
            {
                i.SmtpHost,
                i.SmtpPort,
                i.SmtpUtilizator,
                i.SmtpParolaCriptata,
                i.SmtpEmailFrom,
                i.SmtpNumeFrom,
                i.SmtpSecuritate
            })
            .FirstOrDefaultAsync(ct);

        if (inst is null)
            throw new InvalidOperationException(
                $"Instituția {institutieId} nu există.");

        if (string.IsNullOrWhiteSpace(inst.SmtpHost)
            || !inst.SmtpPort.HasValue
            || string.IsNullOrWhiteSpace(inst.SmtpUtilizator)
            || string.IsNullOrWhiteSpace(inst.SmtpParolaCriptata)
            || string.IsNullOrWhiteSpace(inst.SmtpEmailFrom))
            throw new InvalidOperationException(
                $"Configurare SMTP incompletă pentru instituția {institutieId}.");

        string parola;
        try
        {
            parola = _criptare.Decripteaza(inst.SmtpParolaCriptata);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Parola SMTP a instituției {institutieId} nu poate fi decriptată (cheia s-a schimbat?).", ex);
        }

        return new ConfigSmtpDecriptat(
            inst.SmtpHost!,
            inst.SmtpPort.Value,
            inst.SmtpUtilizator!,
            parola,
            inst.SmtpEmailFrom!,
            inst.SmtpNumeFrom ?? inst.SmtpEmailFrom!,
            inst.SmtpSecuritate);
    }

    private static SecureSocketOptions RezolvaOptiuniSecuritate(SmtpSecuritate securitate, int port)
        => securitate switch
        {
            SmtpSecuritate.StartTls => SecureSocketOptions.StartTls,
            SmtpSecuritate.SslDirect => SecureSocketOptions.SslOnConnect,
            _ => SecureSocketOptions.Auto
        };

    private record ConfigSmtpDecriptat(
        string Host, int Port, string Utilizator, string Parola,
        string EmailFrom, string NumeFrom, SmtpSecuritate Securitate);

    private class ConexiuneSmtp : IConexiuneEmail
    {
        private readonly SmtpClient _client;
        private readonly string _emailFrom;
        private readonly string _numeFrom;
        private readonly ILogger _logger;

        public ConexiuneSmtp(SmtpClient client, string emailFrom, string numeFrom, ILogger logger)
        {
            _client = client;
            _emailFrom = emailFrom;
            _numeFrom = numeFrom;
            _logger = logger;
        }

        public async Task<RezultatTrimitere> TrimiteAsync(
            string emailDestinatar, string subiect, string continutHtml, CancellationToken ct = default)
        {
            try
            {
                var mesaj = new MimeMessage();
                mesaj.From.Add(new MailboxAddress(_numeFrom, _emailFrom));
                mesaj.To.Add(MailboxAddress.Parse(emailDestinatar));
                mesaj.Subject = subiect;
                mesaj.Body = new BodyBuilder { HtmlBody = continutHtml }.ToMessageBody();

                var raspuns = await _client.SendAsync(mesaj, ct);
                return new RezultatTrimitere(true, string.IsNullOrWhiteSpace(raspuns) ? "Trimis" : raspuns);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Eșec trimitere email către {Email}.", emailDestinatar);
                return new RezultatTrimitere(false, $"{ex.GetType().Name}: {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_client.IsConnected)
                    await _client.DisconnectAsync(quit: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Eroare la închiderea conexiunii SMTP.");
            }
            finally
            {
                _client.Dispose();
            }
        }
    }
}