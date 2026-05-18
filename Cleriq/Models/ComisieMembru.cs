namespace Cleriq.Models;

public class ComisieMembru
{
    public int Id { get; set; }

    public int ComisieId { get; set; }
    public Comisie Comisie { get; set; } = null!;

    public int ConsilierId { get; set; }
    public Consilier Consilier { get; set; } = null!;

    public RolComisie Rol { get; set; } = RolComisie.Membru;
}