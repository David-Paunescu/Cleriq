using Cleriq.Models;

namespace Cleriq.DTOs;

public record InregistrareVotDto(int ConsilierId, OptiuneVot Optiune);

public record VotDto(
    int Id,
    int PunctId,
    int ConsilierId,
    string NumeCompletConsilier,
    OptiuneVot Optiune,
    DateTime DataOra,
    int InstitutieId);