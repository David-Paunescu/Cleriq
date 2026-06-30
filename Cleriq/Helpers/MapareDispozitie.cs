using Cleriq.DTOs;
using Cleriq.Models;
using Microsoft.EntityFrameworkCore;

namespace Cleriq.Helpers;

// Sursă unică pentru include-urile + maparea spre DispozitieDetaliiDto. Paralel cu MapareHcl.
public static class MapareDispozitie
{
    // AsNoTracking: reload-uri strict read-only (proiecție spre DTO). Ocolește identity map-ul →
    // după soft-delete-ul unui semnatar (Pas 6), reload-ul nu mai întoarce rândul rămas agățat.
    public static IQueryable<Dispozitie> CuIncludeComplet(this IQueryable<Dispozitie> query) =>
        query
            .AsNoTracking()
            .Include(d => d.Semnatari).ThenInclude(s => s.Persoana)
            .Include(d => d.Semnatari).ThenInclude(s => s.Consilier);
            // .Include(d => d.Comunicari) — adăugat la Pas 10

    public static DispozitieDetaliiDto SpreDetaliiDto(Dispozitie d) => new(
        d.Id, d.Numar, d.AnNumerotare, d.TipDispozitie, d.Titlu, d.Continut,
        d.DataEmitere, d.DataIntrareInVigoare, d.Status,
        d.EstePublicat, d.DataPublicareMol, d.AIntratInCircuit,
        !string.IsNullOrEmpty(d.CaleStocareSemnat), d.NumeFisierSemnat, d.MarimeSemnat, d.DataIncarcareSemnat,
        d.ContrasemnaturaRefuzata, d.ObiectieLegalitateSecretar, d.DataRefuzContrasemnare,
        d.DataInvalidare, d.MotivInvalidare,
        d.DataInvalidare != null ? d.MotivInvalidare.EtichetaDispozitie() : null,
        d.RefInvalidare, d.MotivInvalidareAltulText,
        d.InstitutieId, d.CreatLa,
        d.Semnatari.OrderBy(s => s.OrdineAfisare).Select(SpreSemnatarDto).ToList());

    private static SemnatarDispozitieDto SpreSemnatarDto(SemnatarDispozitie s) => new(
        s.Id, s.RolSemnatar, s.PersoanaId, s.ConsilierId,
        s.Persoana?.NumeComplet ?? s.Consilier?.NumeComplet ?? "—",
        s.DataSemnare, s.OrdineAfisare);
}
