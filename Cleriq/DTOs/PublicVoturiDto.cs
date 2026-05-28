using Cleriq.Models;

namespace Cleriq.DTOs;

public record PublicVoturiPunctDto(
    int PunctId,
    int Ordine,
    string Titlu,
    TipVot TipVot,
    RezultatPunct? Rezultat,
    int Pentru,
    int Impotriva,
    int Abtineri,
    int TotalExprimate,
    List<PublicVotDto> VoturiNominale);   // gol la vot secret

public record PublicVotDto(
    string NumeCompletConsilier,
    OptiuneVot Optiune);