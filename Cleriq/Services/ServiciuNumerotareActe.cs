using Cleriq.Data;
using Cleriq.Helpers;
using Cleriq.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Services;

// Generalizarea numerotării HCL (S49) peste IActNumerotat. Aceeași mecanică
// concurrency-critică (compare-and-swap pe index unic filtrat + retry), un singur
// code-path pentru toate actele cu registru propriu. Mesajele sunt act-neutre („act").
public class ServiciuNumerotareActe : IServiciuNumerotareActe
{
    private const int SqlServerErrorUniqueConstraint = 2601;
    private const int SqlServerErrorDuplicateKey = 2627;

    private readonly AppDbContext _context;

    public ServiciuNumerotareActe(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SugereazaNumarAsync<T>(
        int institutieId, int anNumerotare, CancellationToken ct = default)
        where T : EntitateDeBaza, IActNumerotat
    {
        // IgnoreQueryFilters include și actele soft-deleted — numerele arse contează.
        // Tenant re-impus manual (pattern S47 anti-scurgere cross-tenant).
        var maxNumar = await _context.Set<T>()
            .IgnoreQueryFilters()
            .Where(a => a.InstitutieId == institutieId
                     && a.AnNumerotare == anNumerotare
                     && a.Numar != null)
            .MaxAsync(a => (int?)a.Numar, ct);

        return (maxNumar ?? 0) + 1;
    }

    public async Task<RezultatAtribuireNumar> AtribuieAsync<T>(
        int actId, int numar, bool confirmaCuLacune, CancellationToken ct = default)
        where T : EntitateDeBaza, IActNumerotat
    {
        if (numar <= 0)
            return new RezultatAtribuireNumar(
                TipRezultatAtribuire.NumarInvalid,
                "Numărul trebuie să fie pozitiv.");

        var act = await _context.Set<T>().FirstOrDefaultAsync(a => a.Id == actId, ct);
        if (act is null)
            return new RezultatAtribuireNumar(
                TipRezultatAtribuire.HclInexistent,
                "Act inexistent.");

        if (act.Status != StatusActRedactional.Draft)
            return new RezultatAtribuireNumar(
                TipRezultatAtribuire.StareInvalidaHcl,
                $"Actul în stare {act.Status} — numerotarea se atribuie doar pe Draft.");

        // An de registru = anul juridic LOCAL al datei de referință (nu UTC), consistent cu
        // datele DateOnly de pe mandate. Contează la acte emise după miezul nopții.
        var fusOrar = await _context.Institutii
            .Where(i => i.Id == act.InstitutieId)
            .Select(i => i.FusOrar)
            .FirstAsync(ct);
        var an = act.DataReferintaNumerotare.LaFusOrar(fusOrar).Year;

        // Numerele arse rămân arse — subdecizia S49 (paritar slug-uri instituții)
        var numarArs = await _context.Set<T>()
            .IgnoreQueryFilters()
            .AnyAsync(a => a.InstitutieId == act.InstitutieId
                        && a.AnNumerotare == an
                        && a.Numar == numar
                        && a.EsteSters, ct);

        if (numarArs)
        {
            var sugestieDupaArs = await SugereazaNumarAsync<T>(act.InstitutieId, an, ct);
            return new RezultatAtribuireNumar(
                TipRezultatAtribuire.NumarLuat,
                $"Nr. {numar}/{an} a fost atribuit anterior unui act retras și nu poate fi refolosit (audit). Sugerăm nr. {sugestieDupaArs}.",
                SugestieAlternativa: sugestieDupaArs);
        }

        // Gardă 2 — confirmare lacune dacă numărul propus depășește sugestia
        var sugestie = await SugereazaNumarAsync<T>(act.InstitutieId, an, ct);
        if (numar > sugestie && !confirmaCuLacune)
        {
            var lacune = await DetecteazaLacuneIntreAsync<T>(
                act.InstitutieId, an, sugestie, numar - 1, ct);
            return new RezultatAtribuireNumar(
                TipRezultatAtribuire.LacuneNeconfirmate,
                $"Numerele {string.Join(", ", lacune)} ar fi lăsate libere. Trimite din nou cu confirmaCuLacune=true dacă e intenționat.",
                Lacune: lacune);
        }

        // Gardă 3 — compare-and-swap atomic prin filtered unique
        act.Numar = numar;
        act.AnNumerotare = an;
        act.Status = StatusActRedactional.Numerotat;

        if (!string.IsNullOrEmpty(act.Continut))
            act.Continut = act.Continut.Replace(
                PlaceholderAct.NumarNeatribuit, $"{numar}/{an}");
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
            // Race: alt act a luat numărul între read și write
            _context.Entry(act).State = EntityState.Detached;
            var sugestieNoua = await SugereazaNumarAsync<T>(act.InstitutieId, an, ct);
            return new RezultatAtribuireNumar(
                TipRezultatAtribuire.NumarLuat,
                $"Nr. {numar} este atribuit altui act. Sugerăm nr. {sugestieNoua}.",
                SugestieAlternativa: sugestieNoua);
        }
    }

    public async Task<List<GapNumerotare>> DetecteazaGapuriAsync<T>(
        int institutieId, int anNumerotare, CancellationToken ct = default)
        where T : EntitateDeBaza, IActNumerotat
    {
        var atribuite = await _context.Set<T>()
            .IgnoreQueryFilters()
            .Where(a => a.InstitutieId == institutieId
                     && a.AnNumerotare == anNumerotare
                     && a.Numar != null)
            .Select(a => new { Numar = a.Numar!.Value, a.EsteSters })
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

    private async Task<List<int>> DetecteazaLacuneIntreAsync<T>(
        int institutieId, int an, int rangeStart, int rangeEnd, CancellationToken ct)
        where T : EntitateDeBaza, IActNumerotat
    {
        if (rangeEnd < rangeStart) return new List<int>();

        var existente = await _context.Set<T>()
            .IgnoreQueryFilters()
            .Where(a => a.InstitutieId == institutieId
                     && a.AnNumerotare == an
                     && a.Numar != null
                     && a.Numar >= rangeStart
                     && a.Numar <= rangeEnd)
            .Select(a => a.Numar!.Value)
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
