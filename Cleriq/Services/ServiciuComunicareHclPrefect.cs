using Cleriq.Data;
using Cleriq.Models;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Services;

public class ServiciuComunicareHclPrefect : IServiciuComunicareHclPrefect
{
    // Art. 197 alin. (1) OUG 57/2019 — 10 zile lucrătoare de la adoptare
    private const int TermenComunicareZileLucratoare = 10;

    private readonly AppDbContext _context;
    private readonly ICalculatorZileLucratoare _calculator;

    public ServiciuComunicareHclPrefect(
        AppDbContext context, ICalculatorZileLucratoare calculator)
    {
        _context = context;
        _calculator = calculator;
    }

    public async Task<int> SugereazaNumarOrdineRegistruAsync(
        int institutieId, int anRegistru, CancellationToken ct = default)
    {
        // IgnoreQueryFilters — soft-deleted contează (paritar numerotare HCL)
        var maxNumar = await _context.ComunicariHclPrefect
            .IgnoreQueryFilters()
            .Where(c => c.InstitutieId == institutieId
                     && c.AnRegistru == anRegistru)
            .MaxAsync(c => (int?)c.NumarOrdineInRegistru, ct);

        return (maxNumar ?? 0) + 1;
    }

    public DateOnly CalculeazaDataLimitaComunicare(DateOnly dataAdoptare)
        => _calculator.AdaugaZileLucratoare(dataAdoptare, TermenComunicareZileLucratoare);

    public async Task<List<HclUrgentDto>> ObtineHclUrgentDeComunicatAsync(
        int institutieId, int pragZileRamase, CancellationToken ct = default)
    {
        var astazi = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        // Candidați: Status >= Numerotat, neinvalidate, fără comunicare LIVE în registru.
        // Filtrul global pe subquery exclude comunicările soft-deleted (corect — dacă singura
        // comunicare a fost ștearsă, HCL trebuie recomunicat).
        var candidati = await _context.Hcluri
            .Where(h => h.InstitutieId == institutieId
                     && h.Status >= StatusActRedactional.Numerotat
                     && h.DataInvalidare == null
                     && !_context.ComunicariHclPrefect.Any(c => c.HclId == h.Id))
            .Select(h => new
            {
                h.Id,
                h.Numar,
                h.AnNumerotare,
                h.Titlu,
                h.DataAdoptare,
                h.Status
            })
            .ToListAsync(ct);

        var rezultat = new List<HclUrgentDto>();
        foreach (var h in candidati)
        {
            var dataAdoptareOnly = DateOnly.FromDateTime(h.DataAdoptare.Date);
            var dataLimita = CalculeazaDataLimitaComunicare(dataAdoptareOnly);

            // ZileRamase poate fi negativ (HCL deja depășit termenul) — semnal alarmă UI
            int zileRamase;
            if (astazi <= dataLimita)
                zileRamase = _calculator.CalculeazaZileLucratoarePanaLa(astazi, dataLimita);
            else
                zileRamase = -_calculator.CalculeazaZileLucratoarePanaLa(dataLimita, astazi);

            if (zileRamase <= pragZileRamase)
            {
                rezultat.Add(new HclUrgentDto(
                    h.Id,
                    h.Numar!.Value,
                    h.AnNumerotare!.Value,
                    h.Titlu,
                    dataAdoptareOnly,
                    dataLimita,
                    zileRamase,
                    h.Status));
            }
        }

        return rezultat.OrderBy(r => r.ZileRamase).ToList();
    }
}