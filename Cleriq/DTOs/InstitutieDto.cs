using Cleriq.Models;

namespace Cleriq.DTOs;

public record CreareInstitutieDto(string Denumire, string Judet, string CodSiruta, TipInstitutie Tip);

public record InstitutieDto(
    int Id, string Denumire, string Judet, string CodSiruta,
    TipInstitutie Tip, StatusAbonament StatusAbonament, DateTime CreatLa);