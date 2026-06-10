using Cleriq.Data;
using Cleriq.Models;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Services;

public class WorkerTranscrieri : BackgroundService
{
    private static readonly TimeSpan[] BackoffDurations =
    {
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30)
    };
    private const int NumarMaximIncercari = 3;
    private static readonly TimeSpan TimpLimitaInProces = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkerTranscrieri> _logger;
    private readonly TimeSpan _intervalPolling;

    public WorkerTranscrieri(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkerTranscrieri> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var secunde = config.GetValue<int>("Worker:IntervalTranscriereSecunde", 30);
        _intervalPolling = TimeSpan.FromSeconds(Math.Max(5, secunde));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "WorkerTranscrieri pornit. Interval polling: {Interval}s.",
            _intervalPolling.TotalSeconds);

        try { await CuratareOrfaniAsync(stoppingToken); }
        catch (Exception ex) { _logger.LogError(ex, "Eroare la curățarea orfanilor InProces la pornire."); }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Drain: procesează cât timp există tasks (sequential, constraint VRAM)
                while (await ProcessareTuraAsync(stoppingToken)) { }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare neașteptată în WorkerTranscrieri (tură).");
            }

            try { await Task.Delay(_intervalPolling, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("WorkerTranscrieri oprit.");
    }

    private async Task CuratareOrfaniAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var options = sp.GetRequiredService<DbContextOptions<AppDbContext>>();
        var furnizorUtilizator = sp.GetRequiredService<IFurnizorUtilizator>();

        using var ctx = new AppDbContext(options, new FurnizorTenantSystem(), furnizorUtilizator);

        var prag = DateTime.UtcNow - TimpLimitaInProces;

        // ModificatLa se setează la fiecare claim (InAsteptare → InProces); dacă rămâne stale > 1h,
        // task-ul e orfan (crash, kill forțat, deploy în mijlocul procesării).
        var orfani = await ctx.Transcrieri
            .IgnoreQueryFilters()
            .Where(t => !t.EsteSters
                && t.Status == StatusTranscriere.InProces
                && ((t.ModificatLa != null && t.ModificatLa < prag)
                    || (t.ModificatLa == null && t.CreatLa < prag)))
            .ToListAsync(ct);

        if (orfani.Count == 0) return;

        _logger.LogWarning(
            "Curățare orfani: marchez {Count} transcriere(i) InProces > 1h ca Esuate.",
            orfani.Count);

        foreach (var orfan in orfani)
        {
            orfan.Status = StatusTranscriere.Esuata;
            orfan.UltimaEroare = "Worker restart: task orfan (InProces fără actualizare > 1h).";
        }

        await ctx.SaveChangesAsync(ct);
    }

    // Returnează true dacă a procesat un task — semnal pentru drain loop.
    private async Task<bool> ProcessareTuraAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var options = sp.GetRequiredService<DbContextOptions<AppDbContext>>();
        var furnizorUtilizator = sp.GetRequiredService<IFurnizorUtilizator>();
        var serviciuTranscriere = sp.GetRequiredService<IServiciuTranscriere>();
        var generatorPrompt = sp.GetRequiredService<IGeneratorPromptTranscriere>();
        var stocareAudio = sp.GetRequiredService<IStocareAudio>();

        using var ctx = new AppDbContext(options, new FurnizorTenantSystem(), furnizorUtilizator);

        var acum = DateTime.UtcNow;

        var transcriere = await ctx.Transcrieri
            .IgnoreQueryFilters()
            .Include(t => t.Sedinta)
                .ThenInclude(s => s.Institutie)
            .Where(t => !t.EsteSters
                && t.Status == StatusTranscriere.InAsteptare
                && (t.UrmatoareaIncercareDupa == null || t.UrmatoareaIncercareDupa <= acum)
                && !t.Sedinta.EsteSters)
            .OrderBy(t => t.CreatLa)
            .FirstOrDefaultAsync(ct);

        if (transcriere is null) return false;

        _logger.LogInformation(
            "Procesare transcriere {Id} (sedinta {SedintaId}, încercarea {Incercare}/{Max}).",
            transcriere.Id, transcriere.SedintaId,
            transcriere.NumarIncercari + 1, NumarMaximIncercari);

        // Claim: InAsteptare → InProces. ModificatLa se setează automat prin audit.
        transcriere.Status = StatusTranscriere.InProces;
        try
        {
            await ctx.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex,
                "Conflict de concurență la claim pentru transcrierea {Id}, skip.",
                transcriere.Id);
            return true;
        }

        // Consilieri pentru prompt: Prezenta primary, fallback toți activi.
        var consilieriIds = await ctx.Prezente
            .IgnoreQueryFilters()
            .Where(p => !p.EsteSters
                && p.SedintaId == transcriere.SedintaId
                && (p.Status == StatusPrezenta.Prezent
                    || p.Status == StatusPrezenta.OnlinePrezent))
            .Select(p => p.ConsilierId)
            .ToListAsync(ct);

        List<Consilier> consilieri;
        if (consilieriIds.Count > 0)
        {
            consilieri = await ctx.Consilieri
                .IgnoreQueryFilters()
                .Where(c => !c.EsteSters && consilieriIds.Contains(c.Id))
                .ToListAsync(ct);
        }
        else
        {
            consilieri = await ctx.Consilieri
                .IgnoreQueryFilters()
                .Where(c => !c.EsteSters
                    && c.Activ
                    && c.InstitutieId == transcriere.InstitutieId)
                .ToListAsync(ct);
            _logger.LogInformation(
                "Fără Prezență marcată pentru sedinta {SedintaId}; fallback la {Count} consilieri activi.",
                transcriere.SedintaId, consilieri.Count);
        }

        var continut = generatorPrompt.Genereaza(transcriere.Sedinta, consilieri);

        // Deschidere audio. FileNotFound = non-retriable (fișierul a fost șters/mutat).
        Stream audioStream;
        try
        {
            audioStream = await stocareAudio.DeschideAsync(transcriere.CaleStocareAudio, ct);
        }
        catch (FileNotFoundException)
        {
            await TrateazaEroareAsync(ctx, transcriere, retriable: false,
                "Fișierul audio nu mai există pe disk.", ct);
            return true;
        }

        var numeFisier = Path.GetFileName(transcriere.CaleStocareAudio);
        var startProcesare = DateTime.UtcNow;
        RezultatTranscriere rezultat;

        try
        {
            await using (audioStream)
            {
                rezultat = await serviciuTranscriere.TrimiteAsync(
                    audioStream, numeFisier, continut, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutdown grațios. Lăsăm InProces — cleanup-ul la restart preia.
            _logger.LogInformation(
                "Shutdown în timpul procesării transcrierii {Id}. Va fi reluată la restart.",
                transcriere.Id);
            throw;
        }
        catch (Exception ex)
        {
            // Excepție neașteptată la apel wrapper → retriable conservator.
            var durata = (int)(DateTime.UtcNow - startProcesare).TotalMilliseconds;
            _logger.LogError(ex,
                "Eroare neașteptată apel wrapper Whisper, transcriere {Id} (durata={Durata}ms).",
                transcriere.Id, durata);
            await TrateazaEroareAsync(ctx, transcriere, retriable: true,
                $"{ex.GetType().Name}: {ex.Message}", ct);
            return true;
        }

        var durataFinala = (int)(DateTime.UtcNow - startProcesare).TotalMilliseconds;

        if (rezultat.Succes)
        {
            transcriere.Status = StatusTranscriere.Finalizata;
            transcriere.ContinutBrut = rezultat.ContinutJson;
            transcriere.DurataAudioSecunde = rezultat.DurataAudioSecunde;
            transcriere.PromptFolosit = continut.Prompt;
            transcriere.DataPrimireBrut = DateTime.UtcNow;
            transcriere.UltimaEroare = null;
            transcriere.UrmatoareaIncercareDupa = null;

            await ctx.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Transcriere {Id} finalizată. Procesare={DurataProcesare}ms, audio={DurataAudio}s, conținut={Caractere} caractere.",
                transcriere.Id, durataFinala,
                rezultat.DurataAudioSecunde, rezultat.ContinutJson?.Length ?? 0);
        }
        else
        {
            _logger.LogWarning(
                "Transcriere {Id} eșuată (retriable={Retriable}, durata={Durata}ms). Detalii: {Detalii}.",
                transcriere.Id, rezultat.EsteRetriable, durataFinala, rezultat.Detalii);
            await TrateazaEroareAsync(ctx, transcriere, rezultat.EsteRetriable, rezultat.Detalii, ct);
        }

        return true;
    }

    private async Task TrateazaEroareAsync(
        AppDbContext ctx,
        Transcriere transcriere,
        bool retriable,
        string? detalii,
        CancellationToken ct)
    {
        transcriere.NumarIncercari++;
        transcriere.UltimaEroare = detalii;

        if (!retriable || transcriere.NumarIncercari >= NumarMaximIncercari)
        {
            transcriere.Status = StatusTranscriere.Esuata;
            transcriere.UrmatoareaIncercareDupa = null;
            _logger.LogWarning(
                "Transcriere {Id} marcată Esuata definitiv (retriable={Retriable}, încercări={Incercari}).",
                transcriere.Id, retriable, transcriere.NumarIncercari);
        }
        else
        {
            var idxBackoff = Math.Min(transcriere.NumarIncercari - 1, BackoffDurations.Length - 1);
            transcriere.Status = StatusTranscriere.InAsteptare;
            transcriere.UrmatoareaIncercareDupa = DateTime.UtcNow + BackoffDurations[idxBackoff];
            _logger.LogInformation(
                "Transcriere {Id} reprogramată după {Backoff} (încercarea {Incercare}/{Max}).",
                transcriere.Id, BackoffDurations[idxBackoff],
                transcriere.NumarIncercari, NumarMaximIncercari);
        }

        await ctx.SaveChangesAsync(ct);
    }
}