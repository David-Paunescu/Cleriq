using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class Persoana : EntitateDeBaza, IEntitateCuTenant
{
    [MaxLength(200)]
    public string NumeComplet { get; set; } = "";

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Telefon { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;

    public List<MandatFunctie> MandateFunctie { get; set; } = new();
}