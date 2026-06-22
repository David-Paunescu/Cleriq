using Cleriq.Models;

namespace Cleriq.DTOs;

public record AdaugareSemnatarDto(
    RolSemnatar Rol,
    int? ConsilierId,
    int? PersoanaId,
    int OrdineAfisare);

public record SemnatarHclDto(
    int Id,
    RolSemnatar RolSemnatar,
    int? PersoanaId,
    int? ConsilierId,
    string NumeComplet,
    DateOnly DataSemnare,
    int OrdineAfisare);