namespace Cleriq.DTOs;

public record CreareConsilierDto(
    string NumeComplet,
    string? Email,
    string? Telefon);

public record ActualizareConsilierDto(
    string NumeComplet,
    string? Email,
    string? Telefon,
    bool Activ);

public record ConsilierDto(
    int Id,
    string NumeComplet,
    string? Email,
    string? Telefon,
    bool Activ,
    int InstitutieId,
    DateTime CreatLa);