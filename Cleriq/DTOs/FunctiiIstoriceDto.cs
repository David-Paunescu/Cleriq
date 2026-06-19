using Cleriq.Models;

namespace Cleriq.DTOs;

public record SubiectIstoricDto(int Id, string NumeComplet);

public record ViceprimarIstoricDto(
    int ConsilierId,
    string NumeComplet,
    int MandatFunctieId,
    DateOnly DataInceput,
    DateOnly? DataSfarsit);

public record MembruIstoricDto(int ConsilierId, string NumeComplet, RolComisie Rol);