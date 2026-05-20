using Cleriq.Models;

namespace Cleriq.DTOs;

public record CreareInstitutieCuAdminDto(
    // Datele instituției
    string Denumire,
    string Judet,
    string CodSiruta,
    TipInstitutie Tip,
    // Datele primului Admin
    string EmailAdmin,
    string ParolaAdmin,
    string NumeCompletAdmin);

public record RezultatProvisioningDto(
    int InstitutieId,
    string Denumire,
    int AdminId,
    string EmailAdmin);