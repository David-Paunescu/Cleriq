using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public class Convocare : EntitateDeBaza, IEntitateCuTenant
{
    public int SedintaId { get; set; }
    public Sedinta Sedinta { get; set; } = null!;

    public int ConsilierId { get; set; }
    public Consilier Consilier { get; set; } = null!;

    // Canal: Email
    public StatusTrimitere? EmailStatus { get; set; }
    public DateTime? EmailTrimisLa { get; set; }
    [MaxLength(1000)]
    public string? EmailDetalii { get; set; }

    // Canal: SMS
    public StatusTrimitere? SmsStatus { get; set; }
    public DateTime? SmsTrimisLa { get; set; }
    [MaxLength(1000)]
    public string? SmsDetalii { get; set; }

    [MaxLength(500)]
    public string? Subiect { get; set; }

    public string? EmailHtml { get; set; }

    [MaxLength(500)]
    public string? SmsText { get; set; }

    public int InstitutieId { get; set; }
    public Institutie Institutie { get; set; } = null!;
}