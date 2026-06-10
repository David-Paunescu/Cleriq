namespace Cleriq.DTOs;

public record PublicProcesVerbalDto(
    int SedintaId,
    string? Continut,
    DateTime? DataFinalizare,
    bool EsteSemnat);   // true = există PDF semnat încărcat; portalul poate afișa badge