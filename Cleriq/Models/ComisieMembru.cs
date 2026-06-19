namespace Cleriq.Models;

public class ComisieMembru : EntitateDeBaza, IEntitateCuTenant
{
    public int ComisieId { get; set; }
    public Comisie Comisie { get; set; } = null!;

    public int ConsilierId { get; set; }
    public Consilier Consilier { get; set; } = null!;

    public RolComisie Rol { get; set; } = RolComisie.Membru;

    public DateOnly DataInceput { get; set; }
    public DateOnly? DataSfarsit { get; set; }
    public bool DataInceputEstimata { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}