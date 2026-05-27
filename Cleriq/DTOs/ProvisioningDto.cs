using Cleriq.Models;

namespace Cleriq.DTOs;

public record CreareInstitutieCuAdminDto(
    // Datele instituției
    string Denumire,
    string Judet,
    string CodSiruta,
    TipInstitutie Tip,
    string? Slug,                 // opțional — dacă null/empty, auto-derivăm din Denumire
    // Datele primului Admin
    string EmailAdmin,
    string ParolaAdmin,
    string NumeCompletAdmin);

public record RezultatProvisioningDto(
    int InstitutieId,
    string Denumire,
    string Slug,
    int AdminId,
    string EmailAdmin);

public record EroareSlugDto(string Mesaj, string[] Sugestii);