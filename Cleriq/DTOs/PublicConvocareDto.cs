namespace Cleriq.DTOs;

// Doar nume + status agregat. NU expunem detalii per canal (intern).
public record PublicConvocareDto(
    string NumeCompletConsilier,
    StatusConvocare Status);