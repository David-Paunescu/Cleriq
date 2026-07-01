using Cleriq.Models;

namespace Cleriq.DTOs;

// Body-urile de creare/actualizare sunt act-neutre → reutilizate din ComunicareHclPrefectDto
// (CreareComunicareDto, ActualizareComunicareDto). Aici doar răspunsul, cu DispozitieId.
public record ComunicareDispozitiePrefectDto(
    int Id,
    int DispozitieId,
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
