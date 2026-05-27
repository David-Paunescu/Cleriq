namespace Cleriq.Models;

public class Prezenta : EntitateDeBaza, IEntitateCuTenant
{
    public StatusPrezenta Status { get; set; }
    public DateTime? OraSosire { get; set; }
    public int SedintaId { get; set; }
    public Sedinta Sedinta { get; set; } = null!;
    public int ConsilierId { get; set; }
    public Consilier Consilier { get; set; } = null!;
    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}