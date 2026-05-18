using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class Consilier
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string NumeComplet { get; set; } = "";

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Telefon { get; set; }

    public bool Activ { get; set; } = true;

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
    public List<ComisieMembru> Apartenente { get; set; } = new();
    public List<Mandat> Mandate { get; set; } = new();
}