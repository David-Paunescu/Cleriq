using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class MandatFunctie : EntitateDeBaza, IEntitateCuTenant
{
    public TipFunctie TipFunctie { get; set; }

    public int? PersoanaId { get; set; }
    public Persoana? Persoana { get; set; }

    public int? ConsilierId { get; set; }
    public Consilier? Consilier { get; set; }

    public DateOnly DataInceput { get; set; }
    public DateOnly? DataSfarsit { get; set; }

    [MaxLength(100)]
    public string? NrActNumire { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}