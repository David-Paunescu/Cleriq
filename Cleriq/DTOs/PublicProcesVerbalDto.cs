namespace Cleriq.DTOs;

public record PublicProcesVerbalDto(
    int SedintaId,
    string? Continut,
    DateTime? DataFinalizare,
    bool EsteSemnat,
    int? AprobatInSedintaId,
    string? AprobatInSedintaTitlu,
    DateTime? AprobatInSedintaDataOra);