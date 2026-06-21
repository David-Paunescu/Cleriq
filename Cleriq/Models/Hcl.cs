using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class Hcl : EntitateDeBaza, IEntitateCuTenant
{
    public int? Numar { get; set; }
    public int? AnNumerotare { get; set; }

    public TipHcl TipHcl { get; set; }

    [MaxLength(300)]
    public string Titlu { get; set; } = "";

    public string? Continut { get; set; }

    public DateTime DataAdoptare { get; set; }
    public DateOnly? DataIntrareInVigoare { get; set; }

    public StatusHclRedactional Status { get; set; } = StatusHclRedactional.Draft;

    public int PunctOrdineZiId { get; set; }
    public PunctOrdineZi PunctOrdineZi { get; set; } = null!;

    // Vot snapshot — imutabil după generare
    public int VotPentru { get; set; }
    public int VotImpotriva { get; set; }
    public int VotAbtinere { get; set; }
    public TipMajoritate TipMajoritate { get; set; }

    // Nivel 1 semnătură — variantă semnată extern (paritar PV)
    [MaxLength(500)]
    public string? CaleStocareSemnat { get; set; }
    [MaxLength(255)]
    public string? NumeFisierSemnat { get; set; }
    public long? MarimeSemnat { get; set; }
    [MaxLength(64)]
    public string? HashSha256Semnat { get; set; }
    public DateTime? DataIncarcareSemnat { get; set; }

    // Publicare MOL (Monitorul Oficial Local)
    public DateTime? DataPublicareMol { get; set; }
    public int? PublicataDe { get; set; }

    // Publicare portal public
    public bool EstePublicat { get; set; }

    // Cazul art. 140 alin. 2 (lipsa semnăturii președintelui)
    [MaxLength(500)]
    public string? MotivLipsaSemnaturaPresedinte { get; set; }

    // Axa paralelă — valabilitate juridică
    public DateTime? DataInvalidare { get; set; }
    public MotivInvalidare? MotivInvalidare { get; set; }
    [MaxLength(200)]
    public string? RefInvalidare { get; set; }
    public int? InvalidatDe { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;

    public List<SemnatarHcl> Semnatari { get; set; } = new();
    public List<ComunicareHclPrefect> Comunicari { get; set; } = new();
    public List<Document> Documente { get; set; } = new();
    public List<RelatieHcl> RelatiiSursa { get; set; } = new();
    public List<RelatieHcl> RelatiiTinta { get; set; } = new();
}