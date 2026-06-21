using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class RelatieHcl : EntitateDeBaza, IEntitateCuTenant
{
    // HclSursa NOT NULL — HCL-ul nostru care face acțiunea
    public int HclSursaId { get; set; }
    public Hcl HclSursa { get; set; } = null!;

    // Țintă XOR: ori HclTintaId (HCL intern), ori ReferintaActExternText (act extern)
    public int? HclTintaId { get; set; }
    public Hcl? HclTinta { get; set; }

    public TipRelatieHcl TipRelatie { get; set; }

    [MaxLength(300)]
    public string? ReferintaActExternText { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}