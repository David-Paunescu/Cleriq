using Cleriq.Models;

namespace Cleriq.DTOs;

public record RezultatVotDto(
    int PunctId,
    RezultatPunct Rezultat,
    TipMajoritate? TipMajoritate,
    int TotalConsilieriInFunctie,
    int VoturiPentru,
    int VoturiImpotriva,
    int VoturiAbtinere,
    int TotalVoturiExprimate,
    int PragNecesar);