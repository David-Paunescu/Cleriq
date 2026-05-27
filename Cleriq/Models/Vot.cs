namespace Cleriq.Models;

public class Vot : EntitateDeBaza, IEntitateCuTenant
{
    // TODO (Faza 2): Pentru vot individual, ConsilierId va fi setat din
    // user-ul logat (după adăugare ConsilierId? pe Utilizator).
    // Pentru vot secret, când PunctOrdineZi.TipVot==Secret,
    // GET-urile la voturi vor anonimiza ConsilierId/NumeCompletConsilier.
    public OptiuneVot Optiune { get; set; }
    public DateTime DataOra { get; set; }
    public int PunctId { get; set; }
    public PunctOrdineZi Punct { get; set; } = null!;
    public int ConsilierId { get; set; }
    public Consilier Consilier { get; set; } = null!;
    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}