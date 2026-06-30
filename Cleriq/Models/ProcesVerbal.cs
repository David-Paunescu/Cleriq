using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class ProcesVerbal : EntitateDeBaza, IEntitateCuTenant, IActCuVariantaSemnata
{
    public string? Continut { get; set; }
    public StatusProcesVerbal Status { get; set; } = StatusProcesVerbal.Draft;
    public DateTime? DataGenerare { get; set; }
    public DateTime? DataFinalizare { get; set; }
    public DateTime? DataAprobare { get; set; }
    public int? AprobatDe { get; set; }
    public int? AprobatInSedintaId { get; set; }
    public Sedinta? AprobatInSedinta { get; set; }

    // Varianta semnată (Nivel 1 semnătură): PDF semnat extern (PAdES cu token sau
    // scan al originalului semnat pe hârtie), încărcat de Admin/Secretar.
    // Fișierul fizic stă în ACEEAȘI stocare ca documentele (IStocareDocumente).
    [MaxLength(500)]
    public string? CaleStocareSemnat { get; set; }

    [MaxLength(255)]
    public string? NumeFisierSemnat { get; set; }

    public long? MarimeSemnat { get; set; }

    [MaxLength(64)]
    public string? HashSha256Semnat { get; set; }

    public DateTime? DataIncarcareSemnat { get; set; }

    public int SedintaId { get; set; }
    public Sedinta Sedinta { get; set; } = null!;
    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}