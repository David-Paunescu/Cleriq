using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class Comisie : EntitateDeBaza, IEntitateCuTenant
{
    [MaxLength(200)]
    public string Denumire { get; set; } = "";

    [MaxLength(1000)]
    public string? Descriere { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;

    public List<ComisieMembru> Membri { get; set; } = new();
}