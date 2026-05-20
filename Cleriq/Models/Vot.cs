namespace Cleriq.Models;

public class Vot : EntitateDeBaza, IEntitateCuTenant
{
    public OptiuneVot Optiune { get; set; }
    public DateTime DataOra { get; set; }
    public int PunctId { get; set; }
    public PunctOrdineZi Punct { get; set; } = null!;
    public int ConsilierId { get; set; }
    public Consilier Consilier { get; set; } = null!;
    public int InstitutieId { get; set; }
}