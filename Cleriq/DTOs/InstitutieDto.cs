using Cleriq.Models;

namespace Cleriq.DTOs;

public record InstitutieDto(
    int Id, string Denumire, string Judet, string CodSiruta,
    TipInstitutie Tip, StatusAbonament StatusAbonament, DateTime CreatLa);