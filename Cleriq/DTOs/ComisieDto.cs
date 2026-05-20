using Cleriq.Models;

namespace Cleriq.DTOs;

public record CreareComisieDto(string Denumire, string? Descriere);

public record ActualizareComisieDto(string Denumire, string? Descriere);

public record AdaugareMembruDto(int ConsilierId, RolComisie Rol);

public record MembruComisieDto(int ConsilierId, string NumeComplet, RolComisie Rol);

public record ComisieDto(
    int Id,
    string Denumire,
    string? Descriere,
    int InstitutieId,
    DateTime CreatLa,
    List<MembruComisieDto> Membri);