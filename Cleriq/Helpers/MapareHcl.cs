using Cleriq.DTOs;
using Cleriq.Models;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Helpers;

// Sursă unică pentru include-urile + maparea spre HclDetaliiDto. Partajat de HclController
// (Detalii + toate mutațiile care întorc Detalii) și SemnatariHclController (POST/DELETE
// întorc Detalii — garda „Semnează" depinde de lista de semnatari). Un singur loc = zero
// drift între cele două controllere.
public static class MapareHcl
{
    // AsNoTracking: aceste reload-uri sunt strict read-only (proiecție spre HclDetaliiDto, apoi
    // răspuns). Ocolește identity map-ul → după ce un copil a fost șters (ex. semnatar), reload-ul
    // pe ACELAȘI DbContext nu mai întoarce entitatea ștearsă rămasă agățată în colecția-navigație;
    // în plus, încarcă curat nav-urile (numeComplet corect și imediat după un POST de semnatar).
    public static IQueryable<Hcl> CuIncludeComplet(this IQueryable<Hcl> query) =>
        query
            .AsNoTracking()
            .Include(h => h.Semnatari).ThenInclude(s => s.Persoana)
            .Include(h => h.Semnatari).ThenInclude(s => s.Consilier)
            .Include(h => h.Documente)
            .Include(h => h.RelatiiSursa).ThenInclude(r => r.HclTinta)
            .Include(h => h.RelatiiTinta).ThenInclude(r => r.HclSursa)
            .Include(h => h.Comunicari);

    public static HclDetaliiDto SpreDetaliiDto(Hcl h) => new(
        h.Id, h.Numar, h.AnNumerotare, h.TipHcl, h.Titlu, h.Continut,
        h.DataAdoptare, h.DataIntrareInVigoare, h.Status, h.PunctOrdineZiId,
        h.VotPentru, h.VotImpotriva, h.VotAbtinere, h.TipMajoritate,
        h.EstePublicat, h.DataPublicareMol, h.AIntratInCircuit,
        !string.IsNullOrEmpty(h.CaleStocareSemnat), h.NumeFisierSemnat, h.MarimeSemnat, h.DataIncarcareSemnat,
        h.MotivLipsaSemnaturaPresedinte,
        h.DataInvalidare, h.MotivInvalidare, h.RefInvalidare, h.MotivInvalidareAltulText,
        h.InstitutieId, h.CreatLa,
        h.Semnatari.OrderBy(s => s.OrdineAfisare).Select(SpreSemnatarDto).ToList(),
        h.Documente.OrderBy(d => d.Ordine).Select(SpreDocumentDto).ToList(),
        h.RelatiiSursa.Select(SpreRelatieDto).ToList(),
        h.RelatiiTinta.Select(SpreRelatieDto).ToList(),
        h.Comunicari.OrderByDescending(c => c.NumarOrdineInRegistru).Select(SpreComunicareDto).ToList());

    // Public: folosit și în payload-ul 409 „relații active" din HclController.Invalidare.
    public static RelatieHclDto SpreRelatieDto(RelatieHcl r) => new(
        r.Id, r.TipRelatie,
        r.HclSursaId, FormateazaNumar(r.HclSursa), r.HclSursa?.Titlu ?? "—",
        r.HclTintaId, FormateazaNumar(r.HclTinta), r.HclTinta?.Titlu,
        r.ReferintaActExternText);

    private static SemnatarHclDto SpreSemnatarDto(SemnatarHcl s) => new(
        s.Id, s.RolSemnatar, s.PersoanaId, s.ConsilierId,
        s.Persoana?.NumeComplet ?? s.Consilier?.NumeComplet ?? "—",
        s.DataSemnare, s.OrdineAfisare);

    private static DocumentHclDto SpreDocumentDto(Document d) => new(
        d.Id, d.Denumire, d.Descriere, d.TipDocumentHcl, d.NumarOrdinAnexa,
        d.NumeFisierOriginal, d.Marime, d.Ordine);

    private static ComunicareHclPrefectDto SpreComunicareDto(ComunicareHclPrefect c) => new(
        c.Id, c.HclId, c.NumarOrdineInRegistru, c.AnRegistru,
        c.DataTrimiteri, c.DataInregistrareInRegistru, c.CanalTransmitere,
        c.NrInregistrarePrefect, c.DataConfirmarePrefect, c.ObiectiiMotivate,
        c.RaspunsPrefect, c.DataRaspunsPrefect, c.ObservatiiInterne, c.CreatLa);

    private static string? FormateazaNumar(Hcl? h)
        => h?.Numar != null && h.AnNumerotare != null ? $"{h.Numar}/{h.AnNumerotare}" : null;
}
