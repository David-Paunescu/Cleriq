using Microsoft.EntityFrameworkCore;

namespace Cleriq.Models;

[Index(nameof(PunctId), nameof(ConsilierId), IsUnique = true)]
public class Vot
{
    public int Id { get; set; }
    public OptiuneVot Optiune { get; set; }
    public DateTime DataOra { get; set; }
    public int PunctId { get; set; }
    public PunctOrdineZi Punct { get; set; } = null!;
    public int ConsilierId { get; set; }
    public Consilier Consilier { get; set; } = null!;
}