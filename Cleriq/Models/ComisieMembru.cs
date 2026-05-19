using Microsoft.EntityFrameworkCore;

namespace Cleriq.Models;

[Index(nameof(ComisieId), nameof(ConsilierId), IsUnique = true)]
public class ComisieMembru : EntitateDeBaza, IEntitateCuTenant
{
    public int ComisieId { get; set; }
    public Comisie Comisie { get; set; } = null!;

    public int ConsilierId { get; set; }
    public Consilier Consilier { get; set; } = null!;

    public RolComisie Rol { get; set; } = RolComisie.Membru;
    public int InstitutieId { get; set; }
}