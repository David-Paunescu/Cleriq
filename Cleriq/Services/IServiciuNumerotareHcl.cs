namespace Cleriq.Services;

public enum TipRezultatAtribuire
{
    Succes = 1,
    HclInexistent = 2,
    StareInvalidaHcl = 3,
    NumarInvalid = 4,
    LacuneNeconfirmate = 5,
    NumarLuat = 6
}

public record RezultatAtribuireNumar(
    TipRezultatAtribuire Tip,
    string? MesajEroare = null,
    int? NumarAtribuit = null,
    int? SugestieAlternativa = null,
    List<int>? Lacune = null);

public record GapNumerotare(int Numar, bool EsteArs);

public interface IServiciuNumerotareHcl
{
    Task<int> SugereazaNumarAsync(
        int institutieId, int anNumerotare, CancellationToken ct = default);

    Task<RezultatAtribuireNumar> AtribuieAsync(
        int hclId, int numar, bool confirmaCuLacune, CancellationToken ct = default);

    Task<List<GapNumerotare>> DetecteazaGapuriAsync(
        int institutieId, int anNumerotare, CancellationToken ct = default);
}