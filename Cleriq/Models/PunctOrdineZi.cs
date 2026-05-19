using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class PunctOrdineZi : EntitateDeBaza, IEntitateCuTenant
{
    public int Ordine { get; set; }
    [MaxLength(500)]
    public string Titlu { get; set; } = "";
    public string? Descriere { get; set; }
    public TipPunct Tip { get; set; } = TipPunct.ProiectHCL;
    public bool NecesitaVot { get; set; } = true;
    public TipMajoritate? TipMajoritate { get; set; }
    public RezultatPunct? Rezultat { get; set; }
    public int SedintaId { get; set; }
    public Sedinta Sedinta { get; set; } = null!;
    public List<Vot> Voturi { get; set; } = new();
    public int InstitutieId { get; set; }
}