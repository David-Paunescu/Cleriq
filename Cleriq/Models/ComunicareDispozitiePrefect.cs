using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

// Comunicarea dispoziției la prefect (art. 197 alin. (1) — AMBELE tipuri, 10 zile lucrătoare de la
// emitere). Registru propriu, paralel cu ComunicareHclPrefect (alegere de design, nu mandat legal).
public class ComunicareDispozitiePrefect : EntitateDeBaza, IEntitateCuTenant
{
    public int DispozitieId { get; set; }
    public Dispozitie Dispozitie { get; set; } = null!;

    public int NumarOrdineInRegistru { get; set; }
    public int AnRegistru { get; set; }

    public DateOnly DataTrimiteri { get; set; }
    public DateOnly DataInregistrareInRegistru { get; set; }

    public CanalTransmiterePrefect CanalTransmitere { get; set; }

    [MaxLength(100)]
    public string? NrInregistrarePrefect { get; set; }
    public DateOnly? DataConfirmarePrefect { get; set; }

    // Per art. 197 alin. (3) — obiecții motivate ale prefectului
    [MaxLength(4000)]
    public string? ObiectiiMotivate { get; set; }

    public RaspunsPrefect? RaspunsPrefect { get; set; }
    public DateOnly? DataRaspunsPrefect { get; set; }

    public string? ObservatiiInterne { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}
