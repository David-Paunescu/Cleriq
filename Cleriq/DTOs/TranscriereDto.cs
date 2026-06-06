using Cleriq.Models;

namespace Cleriq.DTOs;

public record TranscriereDto(
    int Id,
    int SedintaId,
    StatusTranscriere Status,
    DateTime? DataPrimireBrut,
    DateTime? DataUltimeiEditari,
    long DimensiuneAudio,
    int? DurataAudioSecunde,
    string ModelFolosit,
    int NumarIncercari,
    DateTime? UrmatoareaIncercareDupa,
    string? UltimaEroare,
    int InstitutieId,
    DateTime CreatLa);

public record TranscriereContinutDto(
    int Id,
    int SedintaId,
    StatusTranscriere Status,
    string? ContinutBrut,
    string? ContinutEditat);

public record EditareTranscriereDto(string ContinutEditat);

public record RezultatVerificareTranscriereDto(
    bool Succes,
    int? LatentaMs,
    string? Status,
    string? Device,
    string? ComputeType,
    string? Detalii);