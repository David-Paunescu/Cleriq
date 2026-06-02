using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class Institutie : EntitateDeBaza
{
    [MaxLength(200)]
    public string Denumire { get; set; } = "";

    [MaxLength(100)]
    public string Slug { get; set; } = "";

    [MaxLength(50)]
    public string Judet { get; set; } = "";

    [MaxLength(10)]
    public string CodSiruta { get; set; } = "";
    public TipInstitutie Tip { get; set; }
    public StatusAbonament StatusAbonament { get; set; } = StatusAbonament.Activ;
    public DateTime? DataExpirare { get; set; }

    [MaxLength(100)]
    public string FusOrar { get; set; } = "Europe/Bucharest";

    [MaxLength(200)]
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    [MaxLength(256)]
    public string? SmtpUtilizator { get; set; }
    [MaxLength(2000)]
    public string? SmtpParolaCriptata { get; set; }
    [MaxLength(256)]
    public string? SmtpEmailFrom { get; set; }
    [MaxLength(200)]
    public string? SmtpNumeFrom { get; set; }
    public SmtpSecuritate SmtpSecuritate { get; set; } = SmtpSecuritate.Auto;
    public List<Consilier> Consilieri { get; set; } = new();
    public List<Comisie> Comisii { get; set; } = new();
    public List<Sedinta> Sedinte { get; set; } = new();
}