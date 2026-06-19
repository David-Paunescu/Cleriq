using Cleriq.Data;
using Cleriq.Models;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Services;

// Capcană EF Core: IgnoreQueryFilters() dezactivează filtrele globale pentru
// ÎNTREG query-ul, inclusiv outer queries dacă e plasat în subquery sau join.
// Separăm query-urile manual și re-impunem tenant pe ramurile cu IgnoreQueryFilters.
//
// Decizie semantică: Persoana/Consilier — IgnoreQueryFilters (vrem și soft-deleted
// pentru audit); MandatFunctie/Mandat/ComisieMembri — respectăm filtrul global
// (rândurile soft-deleted = corecții anulate, NU apar în istoric).
public class ServiciuFunctiiIstorice : IServiciuFunctiiIstorice
{
    private readonly AppDbContext _ctx;

    public ServiciuFunctiiIstorice(AppDbContext ctx)
    {
        _ctx = ctx;
    }

    public Task<Persoana?> CinEPrimarulLa(DateOnly data, CancellationToken ct = default)
        => GasestePersoanaCuFunctie(TipFunctie.Primar, data, ct);

    public Task<Persoana?> CinESecretarulUatLa(DateOnly data, CancellationToken ct = default)
        => GasestePersoanaCuFunctie(TipFunctie.SecretarUat, data, ct);

    private async Task<Persoana?> GasestePersoanaCuFunctie(
        TipFunctie tip, DateOnly data, CancellationToken ct)
    {
        var persoanaId = await _ctx.MandateFunctie
            .Where(m => m.TipFunctie == tip
                     && m.DataInceput <= data
                     && (m.DataSfarsit == null || m.DataSfarsit >= data))
            .Select(m => m.PersoanaId)
            .FirstOrDefaultAsync(ct);

        if (persoanaId is null) return null;

        return await _ctx.Persoane
            .IgnoreQueryFilters()
            .Where(p => p.InstitutieId == _ctx.InstitutieIdCurenta
                     && p.Id == persoanaId.Value)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<(Consilier Consilier, MandatFunctie Mandat)>> CineEViceprimariiLa(
        DateOnly data, CancellationToken ct = default)
    {
        // 1) Mandate de viceprimar active la data (filtru global tenant aplică)
        var mandate = await _ctx.MandateFunctie
            .Where(m => m.TipFunctie == TipFunctie.Viceprimar
                     && m.DataInceput <= data
                     && (m.DataSfarsit == null || m.DataSfarsit >= data))
            .ToListAsync(ct);

        if (mandate.Count == 0) return new();

        var consilieriIds = mandate.Select(m => m.ConsilierId!.Value).Distinct().ToList();

        // 2) Consilieri inclusiv soft-deleted (audit istoric), tenant manual
        var consilieri = await _ctx.Consilieri
            .IgnoreQueryFilters()
            .Where(c => c.InstitutieId == _ctx.InstitutieIdCurenta
                     && consilieriIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        // 3) Filtru „viceprimar fantomă": consilieri cu mandat de consilier valid la data
        var consilieriCuMandatValid = (await _ctx.Mandate
            .Where(mc => consilieriIds.Contains(mc.ConsilierId)
                      && mc.DataInceput <= data
                      && (mc.DataSfarsit == null || mc.DataSfarsit >= data))
            .Select(mc => mc.ConsilierId)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();

        return mandate
            .Where(m => consilieriCuMandatValid.Contains(m.ConsilierId!.Value)
                     && consilieri.ContainsKey(m.ConsilierId!.Value))
            .Select(m => (consilieri[m.ConsilierId!.Value], m))
            .ToList();
    }

    public async Task<List<(Consilier Consilier, RolComisie Rol)>> CineEMembriiComisieiLa(
        int comisieId, DateOnly data, CancellationToken ct = default)
    {
        var membri = await _ctx.ComisieMembri
            .Where(m => m.ComisieId == comisieId
                     && m.DataInceput <= data
                     && (m.DataSfarsit == null || m.DataSfarsit >= data))
            .ToListAsync(ct);

        if (membri.Count == 0) return new();

        var consilieriIds = membri.Select(m => m.ConsilierId).Distinct().ToList();
        var consilieri = await _ctx.Consilieri
            .IgnoreQueryFilters()
            .Where(c => c.InstitutieId == _ctx.InstitutieIdCurenta
                     && consilieriIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        return membri
            .Where(m => consilieri.ContainsKey(m.ConsilierId))
            .Select(m => (consilieri[m.ConsilierId], m.Rol))
            .ToList();
    }

    public async Task<Consilier?> CinePresedinteleComisieiLa(
        int comisieId, DateOnly data, CancellationToken ct = default)
    {
        var membrie = await _ctx.ComisieMembri
            .Where(m => m.ComisieId == comisieId
                     && m.Rol == RolComisie.Presedinte
                     && m.DataInceput <= data
                     && (m.DataSfarsit == null || m.DataSfarsit >= data))
            .FirstOrDefaultAsync(ct);

        if (membrie is null) return null;

        return await _ctx.Consilieri
            .IgnoreQueryFilters()
            .Where(c => c.InstitutieId == _ctx.InstitutieIdCurenta
                     && c.Id == membrie.ConsilierId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<Consilier>> CineEConsilieriiLa(
        DateOnly data, CancellationToken ct = default)
    {
        var consilieriIdsCuMandatValid = await _ctx.Mandate
            .Where(m => m.DataInceput <= data
                     && (m.DataSfarsit == null || m.DataSfarsit >= data))
            .Select(m => m.ConsilierId)
            .Distinct()
            .ToListAsync(ct);

        return await _ctx.Consilieri
            .IgnoreQueryFilters()
            .Where(c => c.InstitutieId == _ctx.InstitutieIdCurenta
                     && consilieriIdsCuMandatValid.Contains(c.Id))
            .ToListAsync(ct);
    }
}