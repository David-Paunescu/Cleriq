using Cleriq.Models;

namespace Cleriq.DTOs;

public record CrearePunctDto(
    int Ordine,
    string Titlu,
    string? Descriere,
    TipPunct Tip,
    bool NecesitaVot,
    TipMajoritate? TipMajoritate);

public record ActualizarePunctDto(
    int Ordine,
    string Titlu,
    string? Descriere,
    TipPunct Tip,
    bool NecesitaVot,
    TipMajoritate? TipMajoritate);

public record PunctOrdineZiDto(
    int Id,
    int SedintaId,
    int Ordine,
    string Titlu,
    string? Descriere,
    TipPunct Tip,
    bool NecesitaVot,
    TipMajoritate? TipMajoritate,
    RezultatPunct? Rezultat,
    int InstitutieId,
    DateTime CreatLa);