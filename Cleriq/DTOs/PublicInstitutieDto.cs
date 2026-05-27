using Cleriq.Models;

namespace Cleriq.DTOs;

// Metadate publice pentru UI portal — fără StatusAbonament, CodSiruta, audit
public record PublicInstitutieDto(
    string Slug,
    string Denumire,
    string Judet,
    TipInstitutie Tip,
    string FusOrar);