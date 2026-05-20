namespace Cleriq.DTOs;

public record CreareMandatDto(
    int ConsilierId,
    DateOnly DataInceput,
    DateOnly? DataSfarsit,
    string? GrupPolitic);

public record ActualizareMandatDto(
    DateOnly DataInceput,
    DateOnly? DataSfarsit,
    string? GrupPolitic);

public record MandatDto(
    int Id,
    int ConsilierId,
    string NumeCompletConsilier,
    DateOnly DataInceput,
    DateOnly? DataSfarsit,
    string? GrupPolitic,
    int InstitutieId,
    DateTime CreatLa);