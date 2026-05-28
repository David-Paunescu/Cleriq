using Cleriq.Models;

namespace Cleriq.DTOs;

public record InregistrareVotDto(int ConsilierId, OptiuneVot Optiune);

public record InregistrareVotSelfDto(OptiuneVot Optiune);

public record VotDto(
    int Id,
    int PunctId,
    int ConsilierId,
    string NumeCompletConsilier,
    OptiuneVot Optiune,
    DateTime DataOra,
    int InstitutieId);

public record VoturiPunctDto(
    int PunctId,
    TipVot TipVot,
    int Pentru,
    int Impotriva,
    int Abtineri,
    int TotalExprimate,
    List<VotDto> VoturiNominale,   // gol la vot secret
    List<string> Participanti);    // cine a votat (turnout) — util mai ales la secret