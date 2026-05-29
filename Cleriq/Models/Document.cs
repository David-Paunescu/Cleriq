using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class Document : EntitateDeBaza, IEntitateCuTenant
{
    [MaxLength(300)]
    public string Denumire { get; set; } = "";

    [MaxLength(1000)]
    public string? Descriere { get; set; }

    public TipDocument TipDocument { get; set; } = TipDocument.Anexa;

    [MaxLength(255)]
    public string NumeFisierOriginal { get; set; } = "";

    [MaxLength(100)]
    public string TipMime { get; set; } = "";

    public long Marime { get; set; }

    [MaxLength(64)]
    public string HashSha256 { get; set; } = "";

    [MaxLength(500)]
    public string CaleStocare { get; set; } = "";

    public bool EstePublic { get; set; }

    public int Ordine { get; set; }

    public int? SedintaId { get; set; }
    public Sedinta? Sedinta { get; set; }

    public int? PunctId { get; set; }
    public PunctOrdineZi? Punct { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}