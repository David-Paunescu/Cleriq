namespace Cleriq.Models;

public class SemnatarDispozitie : EntitateDeBaza, IEntitateCuTenant
{
    public int DispozitieId { get; set; }
    public Dispozitie Dispozitie { get; set; } = null!;

    // XOR Persoana/Consilier (Consilier necesar pt. viceprimar-emitent înlocuitor)
    public int? PersoanaId { get; set; }
    public Persoana? Persoana { get; set; }

    public int? ConsilierId { get; set; }
    public Consilier? Consilier { get; set; }

    public RolSemnatarDispozitie RolSemnatar { get; set; }

    public DateOnly DataSemnare { get; set; }
    public int OrdineAfisare { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}
