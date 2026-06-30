using Cleriq.Models;

namespace Cleriq.Services;

// Serviciu generic de numerotare peste orice IActNumerotat (HCL, Dispoziție). Registru
// propriu per (InstitutieId, AnNumerotare) via index unic filtrat pe tipul concret T.
// Tipurile de rezultat (RezultatAtribuireNumar, TipRezultatAtribuire, GapNumerotare)
// sunt definite în IServiciuNumerotareHcl.cs (partajate).
public interface IServiciuNumerotareActe
{
    Task<int> SugereazaNumarAsync<T>(
        int institutieId, int anNumerotare, CancellationToken ct = default)
        where T : EntitateDeBaza, IActNumerotat;

    Task<RezultatAtribuireNumar> AtribuieAsync<T>(
        int actId, int numar, bool confirmaCuLacune, CancellationToken ct = default)
        where T : EntitateDeBaza, IActNumerotat;

    Task<List<GapNumerotare>> DetecteazaGapuriAsync<T>(
        int institutieId, int anNumerotare, CancellationToken ct = default)
        where T : EntitateDeBaza, IActNumerotat;
}
