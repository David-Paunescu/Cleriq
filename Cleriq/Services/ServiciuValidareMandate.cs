using Cleriq.Data;
using Cleriq.Models;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Services;

public class ServiciuValidareMandate : IServiciuValidareMandate
{
    private readonly AppDbContext _ctx;

    public ServiciuValidareMandate(AppDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<RezultatValidare> PoateFiViceprimar(
        int consilierId, DateOnly dataInceput, DateOnly? dataSfarsit,
        int? mandatExistentId, CancellationToken ct = default)
    {
        bool areMandatAcoperitor;
        if (dataSfarsit is null)
        {
            areMandatAcoperitor = await _ctx.Mandate
                .AnyAsync(m => m.ConsilierId == consilierId
                            && m.DataInceput <= dataInceput
                            && m.DataSfarsit == null, ct);
        }
        else
        {
            var ds = dataSfarsit.Value;
            areMandatAcoperitor = await _ctx.Mandate
                .AnyAsync(m => m.ConsilierId == consilierId
                            && m.DataInceput <= dataInceput
                            && (m.DataSfarsit == null || m.DataSfarsit >= ds), ct);
        }

        return areMandatAcoperitor
            ? new RezultatValidare(true, null)
            : new RezultatValidare(false,
                "Consilierul nu are un mandat de consilier care să acopere perioada propusă pentru funcția de viceprimar.");
    }

    public async Task<RezultatValidare> VerificaOverlap(
        TipFunctie tip, DateOnly dataInceput, DateOnly? dataSfarsit,
        int? persoanaId, int? consilierId, int? mandatExistentId,
        CancellationToken ct = default)
    {
        var query = _ctx.MandateFunctie.Where(m => m.TipFunctie == tip);

        if (tip == TipFunctie.Viceprimar)
        {
            if (consilierId is null)
                return new RezultatValidare(false, "ConsilierId obligatoriu pentru tipul Viceprimar.");
            query = query.Where(m => m.ConsilierId == consilierId);
        }

        if (mandatExistentId.HasValue)
            query = query.Where(m => m.Id != mandatExistentId.Value);

        bool seSuprapun;
        if (dataSfarsit is null)
        {
            seSuprapun = await query.AnyAsync(m =>
                m.DataSfarsit == null || m.DataSfarsit >= dataInceput, ct);
        }
        else
        {
            var ds = dataSfarsit.Value;
            seSuprapun = await query.AnyAsync(m =>
                (m.DataSfarsit == null || m.DataSfarsit >= dataInceput)
                && m.DataInceput <= ds, ct);
        }

        return seSuprapun
            ? new RezultatValidare(false, MesajPentruSuprapunere(tip))
            : new RezultatValidare(true, null);
    }

    private static string MesajPentruSuprapunere(TipFunctie tip) => tip switch
    {
        TipFunctie.Primar => "Există deja un mandat de Primar suprapus pe perioada propusă.",
        TipFunctie.SecretarUat => "Există deja un mandat de Secretar UAT suprapus pe perioada propusă.",
        TipFunctie.Viceprimar => "Consilierul are deja un mandat de Viceprimar suprapus pe perioada propusă.",
        _ => "Există deja un mandat suprapus pe perioada propusă."
    };
}