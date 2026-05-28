using Cleriq.Models;

namespace Cleriq.Helpers;

// Rezumatul voturilor unui punct, cu secretul deja aplicat.
// VoturiNominale e GOL la vot secret — nicio suprafață nu poate emite nume↔opțiune.
// Numărătorile (tally) rămân vizibile mereu: secretul ascunde CINE a votat ce, nu rezultatul.
public record RezumatVoturi(
    bool Secret,
    int Pentru,
    int Impotriva,
    int Abtineri,
    int TotalExprimate,
    IReadOnlyList<Vot> VoturiNominale,
    IReadOnlyList<string> Participanti);

public static class ExtensiiVot
{
    public static RezumatVoturi Rezumat(this PunctOrdineZi punct, IEnumerable<Vot> voturi)
    {
        var lista = voturi.ToList();

        var pentru = lista.Count(v => v.Optiune == OptiuneVot.Pentru);
        var impotriva = lista.Count(v => v.Optiune == OptiuneVot.Impotriva);
        var abtineri = lista.Count(v => v.Optiune == OptiuneVot.Abtinere);
        var total = lista.Count;

        // Defensiv: ignoră voturile al căror consilier a fost soft-deleted (navigation null).
        var cuConsilier = lista.Where(v => v.Consilier is not null).ToList();

        var participanti = cuConsilier
            .Select(v => v.Consilier.NumeComplet)
            .OrderBy(n => n)
            .ToList();

        var secret = punct.TipVot == TipVot.Secret;

        IReadOnlyList<Vot> nominale = secret
            ? Array.Empty<Vot>()
            : cuConsilier.OrderBy(v => v.Consilier.NumeComplet).ToList();

        return new RezumatVoturi(secret, pentru, impotriva, abtineri, total, nominale, participanti);
    }
}