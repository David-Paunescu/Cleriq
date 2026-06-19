namespace Cleriq.DTOs;

public record CrearePersoanaDto(
    string NumeComplet,
    string? Email,
    string? Telefon);

public record ActualizarePersoanaDto(
    string NumeComplet,
    string? Email,
    string? Telefon);

public record PersoanaDto(
    int Id,
    string NumeComplet,
    string? Email,
    string? Telefon,
    int InstitutieId,
    DateTime CreatLa,
    bool AreMandate);