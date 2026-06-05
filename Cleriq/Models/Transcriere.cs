using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class Transcriere : EntitateDeBaza, IEntitateCuTenant
{
    public int SedintaId { get; set; }
    public Sedinta Sedinta { get; set; } = null!;

    public StatusTranscriere Status { get; set; } = StatusTranscriere.InAsteptare;

    // Conținut transcript
    public string? ContinutBrut { get; set; }       // JSON cu segmente, imutabil
    public string? ContinutEditat { get; set; }     // Markdown editat de secretar
    public DateTime? DataPrimireBrut { get; set; }
    public DateTime? DataUltimeiEditari { get; set; }

    // Metadate audio (pentru audit)
    [MaxLength(500)]
    public string CaleStocareAudio { get; set; } = "";
    public long DimensiuneAudio { get; set; }
    public int? DurataAudioSecunde { get; set; }

    // Audit procesare (pentru fine-tune viitor și diagnostic)
    [MaxLength(50)]
    public string ModelFolosit { get; set; } = "large-v2";
    public string? PromptFolosit { get; set; }

    // Retry tracking (sub-decizia 5E, deja pregătit în schemă)
    public int NumarIncercari { get; set; }
    public DateTime? UrmatoareaIncercareDupa { get; set; }
    [MaxLength(1000)]
    public string? UltimaEroare { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}