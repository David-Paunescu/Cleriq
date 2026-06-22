using Cleriq.Models;

namespace Cleriq.DTOs;

public record CreareRelatieDto(
    int? HclTintaId,
    string? ReferintaActExternText,
    TipRelatieHcl TipRelatie);

// Unic pentru ambele direcții: poartă ambele capete, frontend-ul afișează capătul relevant.
// La RelatiiSursa, sursa = HCL-ul curent; la RelatiiTinta, ținta = HCL-ul curent.
public record RelatieHclDto(
    int Id,
    TipRelatieHcl TipRelatie,
    int HclSursaId,
    string? NumarSursaFormatat,
    string TitluSursa,
    int? HclTintaId,
    string? NumarTintaFormatat,
    string? TitluTinta,
    string? ReferintaActExternText);