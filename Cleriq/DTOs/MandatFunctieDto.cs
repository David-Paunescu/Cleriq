using Cleriq.Models;

namespace Cleriq.DTOs;

public record CreareMandatFunctieDto(
    TipFunctie TipFunctie,
    int? PersoanaId,
    int? ConsilierId,
    DateOnly DataInceput,
    DateOnly? DataSfarsit,
    string? NrActNumire);

public record ActualizareMandatFunctieDto(
    DateOnly DataInceput,
    DateOnly? DataSfarsit,
    string? NrActNumire);

public record InchideMandatFunctieDto(DateOnly DataSfarsit);

public record MandatFunctieDto(
    int Id,
    TipFunctie TipFunctie,
    int? PersoanaId,
    string? NumeCompletPersoana,
    int? ConsilierId,
    string? NumeCompletConsilier,
    DateOnly DataInceput,
    DateOnly? DataSfarsit,
    string? NrActNumire,
    int InstitutieId,
    DateTime CreatLa);