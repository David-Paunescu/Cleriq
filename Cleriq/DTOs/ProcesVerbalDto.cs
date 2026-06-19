using Cleriq.Models;

namespace Cleriq.DTOs;

public record EditareProcesVerbalDto(string Continut);

public record AprobareProcesVerbalDto(int AprobatInSedintaId);

public record ProcesVerbalDto(
    int Id,
    int SedintaId,
    string? Continut,
    StatusProcesVerbal Status,
    DateTime? DataGenerare,
    DateTime? DataFinalizare,
    int InstitutieId,
    DateTime CreatLa,
    string? NumeFisierSemnat,
    long? MarimeSemnat,
    DateTime? DataIncarcareSemnat,
    DateTime? DataAprobare,
    int? AprobatInSedintaId,
    string? AprobatInSedintaTitlu,
    DateTime? AprobatInSedintaDataOra);