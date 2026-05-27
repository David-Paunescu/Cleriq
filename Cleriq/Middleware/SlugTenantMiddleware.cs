using System.Text.RegularExpressions;
using Cleriq.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Cleriq.Middleware;

public class SlugTenantMiddleware
{
    public const string CheieItems = "InstitutieId";
    private const string PrefixRutePublice = "/public/";

    // TTL diferențiat: hit pozitiv durabil, hit negativ se auto-vindecă rapid
    private static readonly TimeSpan TtlPozitiv = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan TtlNegativ = TimeSpan.FromMinutes(1);

    // Pre-validare format slug — defense in depth la nivel HTTP, identic cu Slugify
    private static readonly Regex FormatSlugValid = new(
        @"^[a-z0-9]+(-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public SlugTenantMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
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

        // Extrage slug: /public/{slug}/...
        var segmente = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segmente.Length < 2)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var slug = segmente[1];

        // 1. Pre-validare format → 404 fast path (zero DB, zero cache writes)
        if (!FormatSlugValid.IsMatch(slug))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // 2. Cache-aside cu TTL diferențiat
        var cheieCache = $"tenant:slug:{slug}";
        if (!_cache.TryGetValue(cheieCache, out int? institutieId))
        {
            // Cache miss → DB. IgnoreQueryFilters pentru că InstitutieIdCurenta=0 aici
            // (niciun tenant rezolvat încă). Filtru manual pe EsteSters pentru
            // decizia 3 — slug-urile soft-deleted nu rezolvă.
            institutieId = await db.Institutii
                .IgnoreQueryFilters()
                .Where(i => i.Slug == slug && !i.EsteSters)
                .Select(i => (int?)i.Id)
                .FirstOrDefaultAsync();

            var ttl = institutieId.HasValue ? TtlPozitiv : TtlNegativ;
            _cache.Set(cheieCache, institutieId, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
                Size = 1
            });
        }

        // 3. Inexistent → 404
        if (institutieId is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // 4. Setează tenant-ul pentru request și continuă pipeline-ul
        context.Items[CheieItems] = institutieId.Value;
        await _next(context);
    }
}