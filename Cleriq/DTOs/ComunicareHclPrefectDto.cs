using Cleriq.Models;

namespace Cleriq.DTOs;

public record CreareComunicareDto(
    DateOnly DataTrimiteri,
    CanalTransmiterePrefect CanalTransmitere,
    string? NrInregistrarePrefect,
    DateOnly? DataConfirmarePrefect,
    string? ObservatiiInterne);

public record ActualizareComunicareDto(
    RaspunsPrefect? Raspuns,
    DateOnly? DataRaspuns,
    string? ObiectiiMotivate,
    string? ObservatiiInterne,
    string? NrInregistrarePrefect,
    DateOnly? DataConfirmarePrefect);

public record ComunicareHclPrefectDto(
    int Id,
    int HclId,
    int NumarOrdineInRegistru,
    int AnRegistru,
    DateOnly DataTrimiteri,
    DateOnly DataInregistrareInRegistru,
    CanalTransmiterePrefect CanalTransmitere,
    string? NrInregistrarePrefect,
    DateOnly? DataConfirmarePrefect,
    string? ObiectiiMotivate,
    RaspunsPrefect? RaspunsPrefect,
    DateOnly? DataRaspunsPrefect,
    string? ObservatiiInterne,
    DateTime CreatLa);

// Pentru RegistruComunicariPrefectController — vedere cronologică cu denumire HCL
public record RegistruComunicareDto(
    int Id,
    int NumarOrdineInRegistru,
    int AnRegistru,
    DateOnly DataTrimiteri,
    int HclId,
    string? NumarHclFormatat,
    string TitluHcl,
    CanalTransmiterePrefect CanalTransmitere,
    RaspunsPrefect? RaspunsPrefect);