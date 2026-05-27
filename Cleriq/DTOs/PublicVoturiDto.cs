using Cleriq.Models;

namespace Cleriq.DTOs;

public record PublicVoturiPunctDto(
    int PunctId,
    int Ordine,
    string Titlu,
    RezultatPunct? Rezultat,
    List<PublicVotDto> Voturi);

public record PublicVotDto(
    string NumeCompletConsilier,
    OptiuneVot Optiune);