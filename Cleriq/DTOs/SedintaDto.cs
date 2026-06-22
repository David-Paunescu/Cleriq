using Cleriq.Models;

namespace Cleriq.DTOs;

public record CreareSedintaDto(
    string Titlu, string? Numar, TipSedinta Tip,
    DateTime DataOra, string? Loc, ModDesfasurare ModDesfasurare);

public record ActualizareSedintaDto(
    string Titlu, string? Numar, TipSedinta Tip,
    DateTime DataOra, string? Loc, ModDesfasurare ModDesfasurare);

public record SetarePresedinteSedintaDto(int ConsilierId);

public record SedintaDto(
    int Id, string Titlu, string? Numar, TipSedinta Tip, DateTime DataOra,
    string? Loc, ModDesfasurare ModDesfasurare, StatusSedinta Status,
    int InstitutieId, DateTime CreatLa, DateTime? ConvocareTrimisaLa,
    int? PresedinteSedintaConsilierId,
    string? PresedinteSedintaNumeComplet);