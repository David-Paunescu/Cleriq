using Cleriq.Data;
using Cleriq.Models;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Services;

public class WorkerConvocari : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkerConvocari> _logger;
    private readonly TimeSpan _intervalPolling;

    public WorkerConvocari(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkerConvocari> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var secunde = config.GetValue<int>("Worker:IntervalSecunde", 10);
        _intervalPolling = TimeSpan.FromSeconds(Math.Max(1, secunde));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "WorkerConvocari pornit. Interval polling: {Interval}s.",
            _intervalPolling.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessareTuraAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare neașteptată în WorkerConvocari (tură).");
            }

            try
            {
                await Task.Delay(_intervalPolling, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("WorkerConvocari oprit.");
    }

    private async Task ProcessareTuraAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var options = sp.GetRequiredService<DbContextOptions<AppDbContext>>();
        var furnizorUtilizator = sp.GetRequiredService<IFurnizorUtilizator>();
        var email = sp.GetRequiredService<IServiciuNotificareEmail>();
        var sms = sp.GetRequiredService<IServiciuNotificareSms>();

        using var ctx = new AppDbContext(options, new FurnizorTenantSystem(), furnizorUtilizator);

        var convocari = await ctx.Convocari
            .IgnoreQueryFilters()
            .Include(co => co.Sedinta)
            .Where(co => !co.EsteSters
                && (co.EmailStatus == StatusTrimitere.InAsteptare
                    || co.SmsStatus == StatusTrimitere.InAsteptare)
                && (co.Sedinta.Status == StatusSedinta.Convocata
                    || co.Sedinta.Status == StatusSedinta.InDesfasurare))
            .ToListAsync(ct);

        if (convocari.Count == 0) return;

        _logger.LogInformation("Procesez {Count} convocare(s).", convocari.Count);

        var grupuri = convocari.GroupBy(co => co.InstitutieId);

        foreach (var grup in grupuri)
        {
            if (ct.IsCancellationRequested) break;
            await ProcesseazaGrupInstitutieAsync(ctx, grup.Key, grup.ToList(), email, sms, ct);
        }
    }

    private async Task ProcesseazaGrupInstitutieAsync(
        AppDbContext ctx,
        int institutieId,
        List<Convocare> convocari,
        IServiciuNotificareEmail email,
        IServiciuNotificareSms sms,
        CancellationToken ct)
    {
        bool ariNevoiedEmail = convocari.Any(co => co.EmailStatus == StatusTrimitere.InAsteptare);

        IConexiuneEmail? conexiune = null;
        string? eroareConexiune = null;

        if (ariNevoiedEmail)
        {
            try
            {
                conexiune = await email.DeschideConexiuneEmailAsync(institutieId, ct);
            }
            catch (Exception ex)
            {
                eroareConexiune = $"Conexiune SMTP eșuată: {ex.Message}";
                _logger.LogError(ex,
                    "Eșec deschidere conexiune SMTP pentru instituția {Id}. Toate emailurile din grup vor fi marcate eșuate.",
                    institutieId);
            }
        }

        try
        {
            foreach (var co in convocari)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    await ProcesseazaUnaAsync(ctx, co, conexiune, eroareConexiune, sms, ct);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex,
                        "Conflict de concurență pe convocarea {Id}, skip.", co.Id);
                    ctx.Entry(co).State = EntityState.Detached;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Eroare la procesarea convocării {Id}.", co.Id);
                }
            }
        }
        finally
        {
            if (conexiune is not null)
                await conexiune.DisposeAsync();
        }
    }

    private async Task ProcesseazaUnaAsync(
        AppDbContext ctx,
        Convocare co,
        IConexiuneEmail? conexiune,
        string? eroareConexiune,
        IServiciuNotificareSms sms,
        CancellationToken ct)
    {
        var consilier = await ctx.Consilieri
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == co.ConsilierId, ct);

        var acum = DateTime.UtcNow;

        if (consilier is null)
        {
            const string detalii = "Consilier inexistent în DB la momentul trimiterii.";

            if (co.EmailStatus == StatusTrimitere.InAsteptare)
            {
                ctx.IncercariTrimitere.Add(CreeazaIncercare(
                    co, CanalNotificare.Email, StatusIncercare.Esuata, null, detalii));
                co.EmailStatus = StatusTrimitere.Esuata;
                co.EmailDetalii = detalii;
                co.EmailTrimisLa = acum;
            }
            if (co.SmsStatus == StatusTrimitere.InAsteptare)
            {
                ctx.IncercariTrimitere.Add(CreeazaIncercare(
                    co, CanalNotificare.Sms, StatusIncercare.Esuata, null, detalii));
                co.SmsStatus = StatusTrimitere.Esuata;
                co.SmsDetalii = detalii;
                co.SmsTrimisLa = acum;
            }
            await ctx.SaveChangesAsync(ct);
            return;
        }

        if (co.EmailStatus == StatusTrimitere.InAsteptare)
        {
            if (string.IsNullOrWhiteSpace(consilier.Email))
            {
                const string detalii = "Destinație email lipsă la momentul trimiterii.";
                ctx.IncercariTrimitere.Add(CreeazaIncercare(
                    co, CanalNotificare.Email, StatusIncercare.Esuata, null, detalii));
                co.EmailStatus = StatusTrimitere.Esuata;
                co.EmailDetalii = detalii;
                co.EmailTrimisLa = acum;
            }
            else if (conexiune is null)
            {
                var detalii = eroareConexiune ?? "Conexiune SMTP indisponibilă.";
                ctx.IncercariTrimitere.Add(CreeazaIncercare(
                    co, CanalNotificare.Email, StatusIncercare.Esuata, consilier.Email, detalii));
                co.EmailStatus = StatusTrimitere.Esuata;
                co.EmailDetalii = detalii;
                co.EmailTrimisLa = acum;
            }
            else
            {
                var rez = await conexiune.TrimiteAsync(
                    consilier.Email,
                    co.Subiect ?? string.Empty,
                    co.EmailHtml ?? string.Empty,
                    ct);

                var statusIncercare = rez.Succes ? StatusIncercare.Trimisa : StatusIncercare.Esuata;
                ctx.IncercariTrimitere.Add(CreeazaIncercare(
                    co, CanalNotificare.Email, statusIncercare, consilier.Email, rez.Detalii));

                co.EmailStatus = rez.Succes ? StatusTrimitere.Trimisa : StatusTrimitere.Esuata;
                co.EmailDetalii = rez.Detalii;
                co.EmailTrimisLa = acum;
            }
        }

        if (co.SmsStatus == StatusTrimitere.InAsteptare)
        {
            if (string.IsNullOrWhiteSpace(consilier.Telefon))
            {
                const string detalii = "Destinație telefon lipsă la momentul trimiterii.";
                ctx.IncercariTrimitere.Add(CreeazaIncercare(
                    co, CanalNotificare.Sms, StatusIncercare.Esuata, null, detalii));
                co.SmsStatus = StatusTrimitere.Esuata;
                co.SmsDetalii = detalii;
                co.SmsTrimisLa = acum;
            }
            else
            {
                var rez = await sms.TrimiteAsync(
                    co.InstitutieId,
                    consilier.Telefon,
                    co.SmsText ?? string.Empty,
                    ct);

                var statusIncercare = rez.Succes ? StatusIncercare.Trimisa : StatusIncercare.Esuata;
                ctx.IncercariTrimitere.Add(CreeazaIncercare(
                    co, CanalNotificare.Sms, statusIncercare, consilier.Telefon, rez.Detalii));

                co.SmsStatus = rez.Succes ? StatusTrimitere.Trimisa : StatusTrimitere.Esuata;
                co.SmsDetalii = rez.Detalii;
                co.SmsTrimisLa = acum;
            }
        }

        await ctx.SaveChangesAsync(ct);
    }

    private static IncercareTrimitere CreeazaIncercare(
        Convocare co,
        CanalNotificare canal,
        StatusIncercare status,
        string? destinatar,
        string? detalii)
    {
        return new IncercareTrimitere
        {
            ConvocareId = co.Id,
            InstitutieId = co.InstitutieId,
            Canal = canal,
            Status = status,
            Destinatar = destinatar,
            Detalii = detalii
        };
    }
}