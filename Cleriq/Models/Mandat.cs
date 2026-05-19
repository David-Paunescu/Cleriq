using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class Mandat : EntitateDeBaza, IEntitateCuTenant
{
    public DateOnly DataInceput { get; set; }
    public DateOnly? DataSfarsit { get; set; }

    [MaxLength(100)]
    public string? GrupPolitic { get; set; }

    public int ConsilierId { get; set; }
    public Consilier Consilier { get; set; } = null!;
    public int InstitutieId { get; set; }
}