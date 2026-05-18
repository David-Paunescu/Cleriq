using Microsoft.EntityFrameworkCore;

namespace Cleriq.Models;

[Index(nameof(SedintaId), nameof(ConsilierId), IsUnique = true)]
public class Prezenta : EntitateDeBaza
{
    public StatusPrezenta Status { get; set; }

    public DateTime? OraSosire { get; set; }
    public int SedintaId { get; set; }
    public Sedinta Sedinta { get; set; } = null!;
    public int ConsilierId { get; set; }
    public Consilier Consilier { get; set; } = null!;
}