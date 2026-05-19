using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Cleriq.Models;

public class Utilizator : IdentityUser<int>
{
    [MaxLength(200)]
    public string NumeComplet { get; set; } = "";
}