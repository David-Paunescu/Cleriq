using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class IncercareTrimitere : EntitateDeBaza, IEntitateCuTenant
{
    public int ConvocareId { get; set; }
    public Convocare Convocare { get; set; } = null!;

    public CanalNotificare Canal { get; set; }
    public StatusIncercare Status { get; set; }

    [MaxLength(256)]
    public string? Destinatar { get; set; }

    [MaxLength(1000)]
    public string? Detalii { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}