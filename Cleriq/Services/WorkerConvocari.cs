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
        var notificare = sp.GetRequiredService<IServiciuNotificare>();

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

        foreach (var co in convocari)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await ProcesseazaUnaAsync(ctx, co, notificare, ct);
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

    private async Task ProcesseazaUnaAsync(
        AppDbContext ctx, Convocare co, IServiciuNotificare notificare, CancellationToken ct)
    {
        var consilier = await ctx.Consilieri
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == co.ConsilierId, ct);

        var acum = DateTime.UtcNow;

        if (consilier is null)
        {
            if (co.EmailStatus == StatusTrimitere.InAsteptare)
            {
                co.EmailStatus = StatusTrimitere.Esuata;
                co.EmailDetalii = "Consilier inexistent în DB la momentul trimiterii.";
                co.EmailTrimisLa = acum;
            }
            if (co.SmsStatus == StatusTrimitere.InAsteptare)
            {
                co.SmsStatus = StatusTrimitere.Esuata;
                co.SmsDetalii = "Consilier inexistent în DB la momentul trimiterii.";
                co.SmsTrimisLa = acum;
            }
            await ctx.SaveChangesAsync(ct);
            return;
        }

        // ===== Canal Email =====
        if (co.EmailStatus == StatusTrimitere.InAsteptare)
        {
            if (string.IsNullOrWhiteSpace(consilier.Email))
            {
                // Consilierul și-a șters email-ul între POST și worker.
                co.EmailStatus = StatusTrimitere.Esuata;
                co.EmailDetalii = "Destinație email lipsă la momentul trimiterii.";
                co.EmailTrimisLa = acum;
            }
            else
            {
                var rez = await notificare.TrimiteEmailAsync(
                    consilier.Email,
                    co.Subiect ?? string.Empty,
                    co.EmailHtml ?? string.Empty,
                    ct);
                co.EmailStatus = rez.Succes ? StatusTrimitere.Trimisa : StatusTrimitere.Esuata;
                co.EmailDetalii = rez.Detalii;
                co.EmailTrimisLa = acum;
            }
        }

        // ===== Canal SMS =====
        if (co.SmsStatus == StatusTrimitere.InAsteptare)
        {
            if (string.IsNullOrWhiteSpace(consilier.Telefon))
            {
                co.SmsStatus = StatusTrimitere.Esuata;
                co.SmsDetalii = "Destinație telefon lipsă la momentul trimiterii.";
                co.SmsTrimisLa = acum;
            }
            else
            {
                var rez = await notificare.TrimiteSmsAsync(
                    consilier.Telefon,
                    co.SmsText ?? string.Empty,
                    ct);
                co.SmsStatus = rez.Succes ? StatusTrimitere.Trimisa : StatusTrimitere.Esuata;
                co.SmsDetalii = rez.Detalii;
                co.SmsTrimisLa = acum;
            }
        }

        await ctx.SaveChangesAsync(ct);
    }
}