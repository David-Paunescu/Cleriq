using System.Text.RegularExpressions;
using Cleriq.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Cleriq.Middleware;

public class SlugTenantMiddleware
{
    public const string CheieItems = "InstitutieId";
    private const string PrefixRutePublice = "/public/";

    // Negative caching: id 0 nu e niciodată o instituție reală → sentinel sigur.
    private const string SentinelNegativ = "0";

    // TTL diferențiat: hit pozitiv durabil, hit negativ se auto-vindecă rapid
    private static readonly TimeSpan TtlPozitiv = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan TtlNegativ = TimeSpan.FromMinutes(1);

    // Pre-validare format slug — defense in depth la nivel HTTP, identic cu Slugify
    private static readonly Regex FormatSlugValid = new(
        @"^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly ILogger<SlugTenantMiddleware> _logger;

    public SlugTenantMiddleware(
        RequestDelegate next,
        IDistributedCache cache,
        ILogger<SlugTenantMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value ?? "";

        // No-op pentru rute non-publice — costul este o singură comparație de string
        if (!path.StartsWith(PrefixRutePublice, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var segmente = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segmente.Length < 2)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var slug = segmente[1];

        // Pre-validare format → 404 fast path (zero Redis, zero DB)
        if (!FormatSlugValid.IsMatch(slug))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var cheieCache = $"tenant:slug:{slug}";
        var ct = context.RequestAborted;

        // Cache-aside pe Redis. Un eșec Redis NU doboară portalul public:
        // logăm warning și degradăm grațios la lookup direct în DB.
        int? institutieId = null;
        var rezolvatDinCache = false;

        var valoareCache = await CitesteDinCacheAsync(cheieCache, slug, ct);
        if (valoareCache is not null)
        {
            if (valoareCache == SentinelNegativ)
            {
                rezolvatDinCache = true;            // hit negativ: slug inexistent
            }
            else if (int.TryParse(valoareCache, out var idCache))
            {
                institutieId = idCache;
                rezolvatDinCache = true;
            }
            // Valoare coruptă (neparsabilă) → tratăm ca miss și mergem la DB.
        }

        if (!rezolvatDinCache)
        {
            // IgnoreQueryFilters: InstitutieIdCurenta=0 aici (niciun tenant rezolvat încă).
            // Filtru manual pe EsteSters — slug-urile soft-deleted nu rezolvă (slug-uri „arse").
            institutieId = await db.Institutii
                .IgnoreQueryFilters()
                .Where(i => i.Slug == slug && !i.EsteSters)
                .Select(i => (int?)i.Id)
                .FirstOrDefaultAsync(ct);

            await ScrieInCacheAsync(cheieCache, institutieId, slug, ct);
        }

        if (institutieId is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Items[CheieItems] = institutieId.Value;
        await _next(context);
    }

    private async Task<string?> CitesteDinCacheAsync(string cheie, string slug, CancellationToken ct)
    {
        try
        {
            return await _cache.GetStringAsync(cheie, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "Redis indisponibil la citirea slug-ului {Slug}; continui cu DB.", slug);
            return null;
        }
    }

    private async Task ScrieInCacheAsync(string cheie, int? institutieId, string slug, CancellationToken ct)
    {
        try
        {
            await _cache.SetStringAsync(
                cheie,
                institutieId?.ToString() ?? SentinelNegativ,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = institutieId.HasValue ? TtlPozitiv : TtlNegativ
                },
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "Redis indisponibil la scrierea slug-ului {Slug}; cache sărit.", slug);
        }
    }
}