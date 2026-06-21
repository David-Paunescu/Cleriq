namespace Cleriq.Models;

public class SemnatarHcl : EntitateDeBaza, IEntitateCuTenant
{
    public int HclId { get; set; }
    public Hcl Hcl { get; set; } = null!;

    // XOR Persoana/Consilier (paritar MandatFunctie)
    public int? PersoanaId { get; set; }
    public Persoana? Persoana { get; set; }

    public int? ConsilierId { get; set; }
    public Consilier? Consilier { get; set; }

    public RolSemnatar RolSemnatar { get; set; }

    public DateOnly DataSemnare { get; set; }
    public int OrdineAfisare { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}