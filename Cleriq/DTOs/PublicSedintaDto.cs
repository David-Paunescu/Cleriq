using Cleriq.Models;

namespace Cleriq.DTOs;

// DTO pentru portalul public — fără InstitutieId, audit, sau alte câmpuri interne.
// OrdineDeZi e populat doar la endpoint-ul de detalii; la listă rămâne null.
public record PublicSedintaDto(
    int Id,
    string Titlu,
    string? Numar,
    TipSedinta Tip,
    DateTime DataOra,                        // UTC, frontend convertește la fus orar
    string? Loc,
    ModDesfasurare ModDesfasurare,
    StatusSedinta Status,
    List<PublicPunctOrdineZiDto>? OrdineDeZi);

public record PublicPunctOrdineZiDto(
    int Id,
    int Ordine,
    string Titlu,
    string? Descriere,
    TipPunct Tip,
    bool NecesitaVot,
    TipVot TipVot,
    TipMajoritate? TipMajoritate,
    RezultatPunct? Rezultat);