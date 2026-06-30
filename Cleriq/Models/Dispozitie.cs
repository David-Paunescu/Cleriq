using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cleriq.Models;

// Dispoziția primarului — act administrativ unilateral (art. 196 alin. (1) lit. b) Cod adm.).
// Același ciclu Draft→Numerotat→Semnat ca HCL, dar fără vot/punct de ordine de zi.
public class Dispozitie : EntitateDeBaza, IEntitateCuTenant, IActNumerotat
{
    public int? Numar { get; set; }
    public int? AnNumerotare { get; set; }

    public TipDispozitie TipDispozitie { get; set; }

    [MaxLength(300)]
    public string Titlu { get; set; } = "";

    public string? Continut { get; set; }

    public DateTime DataEmitere { get; set; }
    public DateOnly? DataIntrareInVigoare { get; set; }

    public StatusActRedactional Status { get; set; } = StatusActRedactional.Draft;

    // IActNumerotat — alias in-memory pentru anul de registru (vezi ServiciuNumerotareActe).
    [NotMapped]
    public DateTime DataReferintaNumerotare => DataEmitere;

    // Nivel 1 semnătură — variantă semnată extern (paritar HCL/PV)
    [MaxLength(500)]
    public string? CaleStocareSemnat { get; set; }
    [MaxLength(255)]
    public string? NumeFisierSemnat { get; set; }
    public long? MarimeSemnat { get; set; }
    [MaxLength(64)]
    public string? HashSha256Semnat { get; set; }
    public DateTime? DataIncarcareSemnat { get; set; }

    // Publicare MOL — relevantă doar la Normativ; la Individual = nepublic implicit + override (Pas 9)
    public DateOnly? DataPublicareMol { get; set; }
    public int? PublicataDe { get; set; }

    // Latch „intrat în circuit" — paritar HCL (publicare normativ / comunicare destinatar /
    // comunicare prefect, cel mai timpuriu). Îngheață varianta semnată definitiv.
    public bool AIntratInCircuit { get; set; }

    public bool EstePublicat { get; set; }

    // Contrasemnătură refuzată (obiecție de legalitate motivată) — gatează Semneaza (Pas 6).
    // Primarul poate emite pe răspundere proprie peste refuz (art. 197 alin. (3)).
    public bool ContrasemnaturaRefuzata { get; set; }
    [MaxLength(2000)]
    public string? ObiectieLegalitateSecretar { get; set; }
    public int? RefuzContrasemnareDe { get; set; }
    public DateTime? DataRefuzContrasemnare { get; set; }

    // Axa paralelă — valabilitate juridică (reuse MotivInvalidare; regula de revocare la Pas 8)
    public DateTime? DataInvalidare { get; set; }
    public MotivInvalidare? MotivInvalidare { get; set; }
    [MaxLength(200)]
    public string? RefInvalidare { get; set; }
    [MaxLength(300)]
    public string? MotivInvalidareAltulText { get; set; }
    public int? InvalidatDe { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;

    public List<SemnatarDispozitie> Semnatari { get; set; } = new();
    // Comunicari (List<ComunicareDispozitiePrefect>) — adăugat la Pas 10
}
