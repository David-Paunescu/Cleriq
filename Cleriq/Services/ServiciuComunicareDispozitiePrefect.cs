using Cleriq.Data;
using Cleriq.Models;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Services;

// Paralel cu ServiciuComunicareHclPrefect (decizia #8 — nu generalizat). Diferă doar sursa datei de
// referință (DataEmitere vs. DataAdoptare) și registrul propriu de dispoziții.
public class ServiciuComunicareDispozitiePrefect : IServiciuComunicareDispozitiePrefect
{
    // Art. 197 alin. (1) OUG 57/2019 — 10 zile lucrătoare de la emitere
    private const int TermenComunicareZileLucratoare = 10;

    private readonly AppDbContext _context;
    private readonly ICalculatorZileLucratoare _calculator;

    public ServiciuComunicareDispozitiePrefect(
        AppDbContext context, ICalculatorZileLucratoare calculator)
    {
        _context = context;
        _calculator = calculator;
    }

    public async Task<int> SugereazaNumarOrdineRegistruAsync(
        int institutieId, int anRegistru, CancellationToken ct = default)
    {
        // IgnoreQueryFilters — soft-deleted contează (numărul rămâne ars în registru, paritar HCL)
        var maxNumar = await _context.ComunicariDispozitiePrefect
            .IgnoreQueryFilters()
            .Where(c => c.InstitutieId == institutieId
                     && c.AnRegistru == anRegistru)
            .MaxAsync(c => (int?)c.NumarOrdineInRegistru, ct);

        return (maxNumar ?? 0) + 1;
    }

    public DateOnly CalculeazaDataLimitaComunicare(DateOnly dataEmitere)
        => _calculator.AdaugaZileLucratoare(dataEmitere, TermenComunicareZileLucratoare);

    public async Task<List<DispozitieUrgentDto>> ObtineDispozitiiUrgentDeComunicatAsync(
        int institutieId, int pragZileRamase, CancellationToken ct = default)
    {
        var astazi = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        // Candidați: Status >= Numerotat, neinvalidate, fără comunicare LIVE în registru.
        // Filtrul global pe subquery exclude comunicările soft-deleted (dacă singura comunicare a
        // fost ștearsă, dispoziția trebuie recomunicată).
        var candidati = await _context.Dispozitii
            .Where(d => d.InstitutieId == institutieId
                     && d.Status >= StatusActRedactional.Numerotat
                     && d.DataInvalidare == null
                     && !_context.ComunicariDispozitiePrefect.Any(c => c.DispozitieId == d.Id))
            .Select(d => new
            {
                d.Id,
                d.Numar,
                d.AnNumerotare,
                d.Titlu,
                d.DataEmitere,
                d.Status,
                d.TipDispozitie
            })
            .ToListAsync(ct);

        var rezultat = new List<DispozitieUrgentDto>();
        foreach (var d in candidati)
        {
            var dataEmitereOnly = DateOnly.FromDateTime(d.DataEmitere.Date);
            var dataLimita = CalculeazaDataLimitaComunicare(dataEmitereOnly);

            // ZileRamase poate fi negativ (termen deja depășit) — semnal de alarmă în UI
            int zileRamase;
            if (astazi <= dataLimita)
                zileRamase = _calculator.CalculeazaZileLucratoarePanaLa(astazi, dataLimita);
            else
                zileRamase = -_calculator.CalculeazaZileLucratoarePanaLa(dataLimita, astazi);

            if (zileRamase <= pragZileRamase)
            {
                rezultat.Add(new DispozitieUrgentDto(
                    d.Id,
                    d.Numar!.Value,
                    d.AnNumerotare!.Value,
                    d.Titlu,
                    dataEmitereOnly,
                    dataLimita,
                    zileRamase,
                    d.Status,
                    d.TipDispozitie));
            }
        }

        return rezultat.OrderBy(r => r.ZileRamase).ToList();
    }
}
