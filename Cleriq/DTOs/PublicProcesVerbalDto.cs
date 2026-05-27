namespace Cleriq.DTOs;

public record PublicProcesVerbalDto(
    int SedintaId,
    string? Continut,
    DateTime? DataFinalizare);   // momentul publicării oficiale