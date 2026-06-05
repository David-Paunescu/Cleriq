using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class Sedinta : EntitateDeBaza, IEntitateCuTenant
{
    [MaxLength(300)]
    public string Titlu { get; set; } = "";
    [MaxLength(50)]
    public string? Numar { get; set; }
    public TipSedinta Tip { get; set; }
    public DateTime DataOra { get; set; }
    [MaxLength(300)]
    public string? Loc { get; set; }
    public ModDesfasurare ModDesfasurare { get; set; } = ModDesfasurare.Fizic;
    public StatusSedinta Status { get; set; } = StatusSedinta.Planificata;
    public DateTime? ConvocareTrimisaLa { get; set; }
    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
    public List<PunctOrdineZi> Puncte { get; set; } = new();
    public List<Prezenta> Prezente { get; set; } = new();
    public ProcesVerbal? ProcesVerbal { get; set; }
    public Transcriere? Transcriere { get; set; }
    public List<Convocare> Convocari { get; set; } = new();
    public List<Document> Documente { get; set; } = new();
}