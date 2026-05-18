using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class Institutie : EntitateDeBaza
{
    [MaxLength(200)]
    public string Denumire { get; set; } = "";
    [MaxLength(50)]
    public string Judet { get; set; } = "";
    [MaxLength(10)]
    public string CodSiruta { get; set; } = "";
    public TipInstitutie Tip { get; set; }
    public StatusAbonament StatusAbonament { get; set; } = StatusAbonament.Activ;
    public DateTime? DataExpirare { get; set; }
    public List<Consilier> Consilieri { get; set; } = new();
    public List<Comisie> Comisii { get; set; } = new();
    public List<Sedinta> Sedinte { get; set; } = new();
}