using Cleriq.Models;

namespace Cleriq.DTOs;

public record CrearePunctDto(
    int Ordine,
    string Titlu,
    string? Descriere,
    TipPunct Tip,
    bool NecesitaVot,
    TipMajoritate? TipMajoritate,
    TipVot? TipVot);

public record ActualizarePunctDto(
    int Ordine,
    string Titlu,
    string? Descriere,
    TipPunct Tip,
    bool NecesitaVot,
    TipMajoritate? TipMajoritate,
    TipVot? TipVot);

public record PunctOrdineZiDto(
    int Id,
    int SedintaId,
    int Ordine,
    string Titlu,
    string? Descriere,
    TipPunct Tip,
    bool NecesitaVot,
    TipVot TipVot,
    TipMajoritate? TipMajoritate,
    RezultatPunct? Rezultat,
    int InstitutieId,
    DateTime CreatLa);