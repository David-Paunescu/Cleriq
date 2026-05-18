namespace Cleriq.Models;

public class ProcesVerbal : EntitateDeBaza
{
    public string? Continut { get; set; }
    public StatusProcesVerbal Status { get; set; } = StatusProcesVerbal.Draft;
    public DateTime? DataGenerare { get; set; }
    public DateTime? DataFinalizare { get; set; }
    public int SedintaId { get; set; }
    public Sedinta Sedinta { get; set; } = null!;
}