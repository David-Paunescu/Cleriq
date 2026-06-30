using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

// Jurnal de audit append-only pentru acțiuni sensibile pe HCL (deocamdată: anularea
// publicării în MOL). Cine/când vin din EntitateDeBaza (CreatDe/CreatLa, auto-populate).
// Nu există endpoint de modificare/ștergere — imutabil prin lipsa mutațiilor.
public class IstoricActiuneHcl : EntitateDeBaza, IEntitateCuTenant
{
    public int HclId { get; set; }
    public Hcl Hcl { get; set; } = null!;

    public TipActiuneHcl Tip { get; set; }

    [MaxLength(1000)]
    public string Motiv { get; set; } = "";

    [MaxLength(64)]
    public string? AdresaIp { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}
