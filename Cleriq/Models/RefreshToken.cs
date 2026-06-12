using System.ComponentModel.DataAnnotations;

namespace Cleriq.Models;

// Infrastructură de securitate (ca tabelele Identity) — intenționat NU moștenește EntitateDeBaza:
// revocarea înlocuiește soft-delete-ul, fără filtru tenant, cleanup = ștergere fizică.
public class RefreshToken
{
    public int Id { get; set; }

    public int UtilizatorId { get; set; }
    public Utilizator Utilizator { get; set; } = null!;

    public Guid Familie { get; set; }

    [MaxLength(64)]
    public string TokenHash { get; set; } = "";

    public DateTime CreatLa { get; set; }
    public DateTime ExpiraLa { get; set; }
    public DateTime? FolositLa { get; set; }
    public DateTime? RevocatLa { get; set; }
}