using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

public abstract class EntitateDeBaza
{
    public int Id { get; set; }

    public DateTime CreatLa { get; set; }
    public int? CreatDe { get; set; }

    public DateTime? ModificatLa { get; set; }
    public int? ModificatDe { get; set; }

    public bool EsteSters { get; set; }
    public DateTime? StersLa { get; set; }
    public int? StersDe { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}