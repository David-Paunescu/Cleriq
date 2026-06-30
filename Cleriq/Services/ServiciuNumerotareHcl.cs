using Cleriq.Models;

namespace Cleriq.Services;

// Fațadă subțire peste serviciul generic de numerotare, fixată pe Hcl. Păstrată pentru
// ca HclController + TesteNumerotareHcl să rămână neatinse — toată logica trăiește în
// ServiciuNumerotareActe.
public class ServiciuNumerotareHcl : IServiciuNumerotareHcl
{
    private readonly IServiciuNumerotareActe _numerotare;

    public ServiciuNumerotareHcl(IServiciuNumerotareActe numerotare)
    {
        _numerotare = numerotare;
    }

    public Task<int> SugereazaNumarAsync(
        int institutieId, int anNumerotare, CancellationToken ct = default)
        => _numerotare.SugereazaNumarAsync<Hcl>(institutieId, anNumerotare, ct);

    public Task<RezultatAtribuireNumar> AtribuieAsync(
        int hclId, int numar, bool confirmaCuLacune, CancellationToken ct = default)
        => _numerotare.AtribuieAsync<Hcl>(hclId, numar, confirmaCuLacune, ct);

    public Task<List<GapNumerotare>> DetecteazaGapuriAsync(
        int institutieId, int anNumerotare, CancellationToken ct = default)
        => _numerotare.DetecteazaGapuriAsync<Hcl>(institutieId, anNumerotare, ct);
}
