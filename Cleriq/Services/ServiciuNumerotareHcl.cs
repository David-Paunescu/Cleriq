using Cleriq.Data;
using Cleriq.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Cleriq.Helpers;

namespace Cleriq.Services;

public class ServiciuNumerotareHcl : IServiciuNumerotareHcl
{
    private const int SqlServerErrorUniqueConstraint = 2601;
    private const int SqlServerErrorDuplicateKey = 2627;

    private readonly AppDbContext _context;

    public ServiciuNumerotareHcl(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SugereazaNumarAsync(
        int institutieId, int anNumerotare, CancellationToken ct = default)
    {
        // IgnoreQueryFilters include și HCL-urile soft-deleted — numerele arse contează.
        // Tenant re-impus manual (pattern S47 anti-scurgere cross-tenant).
        var maxNumar = await _context.Hcluri
            .IgnoreQueryFilters()
            .Where(h => h.InstitutieId == institutieId
                     && h.AnNumerotare == anNumerotare
                     && h.Numar != null)
            .MaxAsync(h => (int?)h.Numar, ct);

        return (maxNumar ?? 0) + 1;
    }

    public async Task<RezultatAtribuireNumar> AtribuieAsync(
        int hclId, int numar, bool confirmaCuLacune, CancellationToken ct = default)
    {
        if (numar <= 0)
            return new RezultatAtribuireNumar(
                TipRezultatAtribuire.NumarInvalid,
                "Numărul trebuie să fie pozitiv.");

        var hcl = await _context.Hcluri.FirstOrDefaultAsync(h => h.Id == hclId, ct);
        if (hcl is null)
            return new RezultatAtribuireNumar(
                TipRezultatAtribuire.HclInexistent,
                "HCL inexistent.");

        if (hcl.Status != StatusHclRedactional.Draft)
            return new RezultatAtribuireNumar(
                TipRezultatAtribuire.StareInvalidaHcl,
                $"HCL în stare {hcl.Status} — numerotarea se atribuie doar pe Draft.");

        // An de registru = anul juridic LOCAL al adoptării (nu UTC), consistent cu
        // datele DateOnly de pe mandate. Contează la ședințe de îndată după miezul nopții.
        var fusOrar = await _context.Institutii
            .Where(i => i.Id == hcl.InstitutieId)
            .Select(i => i.FusOrar)
            .FirstAsync(ct);
        var an = hcl.DataAdoptare.LaFusOrar(fusOrar).Year;

        // Numerele arse rămân arse — subdecizia S49 (paritar slug-uri instituții)
        var numarArs = await _context.Hcluri
            .IgnoreQueryFilters()
            .AnyAsync(h => h.InstitutieId == hcl.InstitutieId
                        && h.AnNumerotare == an
                        && h.Numar == numar
                        && h.EsteSters, ct);

        if (numarArs)
        {
            var sugestieDupaArs = await SugereazaNumarAsync(hcl.InstitutieId, an, ct);
            return new RezultatAtribuireNumar(
                TipRezultatAtribuire.NumarLuat,
                $"Nr. {numar}/{an} a fost atribuit anterior unui HCL retras și nu poate fi refolosit (audit). Sugerăm nr. {sugestieDupaArs}.",
                SugestieAlternativa: sugestieDupaArs);
        }

        // Gardă 2 — confirmare lacune dacă numarul propus depășește sugestia
        var sugestie = await SugereazaNumarAsync(hcl.InstitutieId, an, ct);
        if (numar > sugestie && !confirmaCuLacune)
        {
            var lacune = await DetecteazaLacuneIntreAsync(
                hcl.InstitutieId, an, sugestie, numar - 1, ct);
            return new RezultatAtribuireNumar(
                TipRezultatAtribuire.LacuneNeconfirmate,
                $"Numerele {string.Join(", ", lacune)} ar fi lăsate libere. Trimite din nou cu confirmaCuLacune=true dacă e intenționat.",
                Lacune: lacune);
        }

        // Gardă 3 — compare-and-swap atomic prin filtered unique
        hcl.Numar = numar;
        hcl.AnNumerotare = an;
        hcl.Status = StatusHclRedactional.Numerotat;

        if (!string.IsNullOrEmpty(hcl.Continut))
            hcl.Continut = hcl.Continut.Replace(
                PlaceholderHcl.NumarNeatribuit, $"{numar}/{an}");
        try
        {
            await _context.SaveChangesAsync(ct);
            return new RezultatAtribuireNumar(
                TipRezultatAtribuire.Succes,
                NumarAtribuit: numar);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx
            && (sqlEx.Number == SqlServerErrorUniqueConstraint
                || sqlEx.Number == SqlServerErrorDuplicateKey))
        {
            // Race: alt HCL a luat numărul între read și write
            _context.Entry(hcl).State = EntityState.Detached;
            var sugestieNoua = await SugereazaNumarAsync(hcl.InstitutieId, an, ct);
            return new RezultatAtribuireNumar(
                TipRezultatAtribuire.NumarLuat,
                $"Nr. {numar} este atribuit altui HCL. Sugerăm nr. {sugestieNoua}.",
                SugestieAlternativa: sugestieNoua);
        }
    }

    public async Task<List<GapNumerotare>> DetecteazaGapuriAsync(
        int institutieId, int anNumerotare, CancellationToken ct = default)
    {
        var atribuite = await _context.Hcluri
            .IgnoreQueryFilters()
            .Where(h => h.InstitutieId == institutieId
                     && h.AnNumerotare == anNumerotare
                     && h.Numar != null)
            .Select(h => new { Numar = h.Numar!.Value, h.EsteSters })
            .ToListAsync(ct);

        if (atribuite.Count == 0) return new List<GapNumerotare>();

        var dictNumar = atribuite.ToDictionary(a => a.Numar, a => a.EsteSters);
        var maxNumar = dictNumar.Keys.Max();

        var gapuri = new List<GapNumerotare>();
        for (int n = 1; n <= maxNumar; n++)
        {
            if (!dictNumar.ContainsKey(n))
                gapuri.Add(new GapNumerotare(n, EsteArs: false));
            else if (dictNumar[n])
                gapuri.Add(new GapNumerotare(n, EsteArs: true));
        }

        return gapuri;
    }

    private async Task<List<int>> DetecteazaLacuneIntreAsync(
        int institutieId, int an, int rangeStart, int rangeEnd, CancellationToken ct)
    {
        if (rangeEnd < rangeStart) return new List<int>();

        var existente = await _context.Hcluri
            .IgnoreQueryFilters()
            .Where(h => h.InstitutieId == institutieId
                     && h.AnNumerotare == an
                     && h.Numar != null
                     && h.Numar >= rangeStart
                     && h.Numar <= rangeEnd)
            .Select(h => h.Numar!.Value)
            .ToListAsync(ct);

        var setExistente = new HashSet<int>(existente);
        var lacune = new List<int>();
        for (int n = rangeStart; n <= rangeEnd; n++)
        {
            if (!setExistente.Contains(n))
                lacune.Add(n);
        }
        return lacune;
    }
}