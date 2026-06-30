using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

// Jurnal de audit append-only, cross-act (HCL, Dispoziție…), identificat prin (TipAct, ActId)
// ca REFERINȚĂ SLABĂ — fără FK/navigație către act, ca să supraviețuiască soft-delete-ului
// actului (log imutabil). Integritatea practică vine din soft-delete-everywhere. Cine/când
// vin din EntitateDeBaza (CreatDe/CreatLa). Nu există endpoint de mutație — imutabil.
public class IstoricActiuneAct : EntitateDeBaza, IEntitateCuTenant
{
    public TipAct TipAct { get; set; }
    public int ActId { get; set; }

    public TipActiuneAct Tip { get; set; }

    [MaxLength(1000)]
    public string Motiv { get; set; } = "";

    [MaxLength(64)]
    public string? AdresaIp { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}
