using Cleriq.Models;

namespace Cleriq.DTOs;

// === Body-uri pentru endpoint-urile DispozitiiController ===

// EmitentConsilierId opțional = override viceprimar (înlocuitor de drept, art. 163); când e null,
// emitentul se derivă din CinEPrimarulLa(DataEmitere).
public record CreareDispozitieDto(
    TipDispozitie TipDispozitie,
    string Titlu,
    DateOnly DataEmitere,
    int? EmitentConsilierId);

public record EditareContinutDispozitieDto(string Continut);

public record AtribuireNumarDispozitieDto(int Numar, bool ConfirmaCuLacune);

public record RefuzContrasemnareDto(string ObiectieLegalitate);

// Fără ConfirmaCuRelatiiActive (paritar HCL) — relațiile între dispoziții sunt Faza 7.
public record InvalidareDispozitieDto(
    MotivInvalidare Motiv,
    string? MotivAltulText,
    string? RefInvalidare);

// === Răspunsuri ===

public record DispozitieDto(
    int Id,
    int? Numar,
    int? AnNumerotare,
    TipDispozitie TipDispozitie,
    string Titlu,
    DateTime DataEmitere,
    DateOnly? DataIntrareInVigoare,
    StatusActRedactional Status,
    bool EstePublicat,
    DateOnly? DataPublicareMol,
    DateTime? DataInvalidare,
    MotivInvalidare? MotivInvalidare,
    int InstitutieId,
    DateTime CreatLa);

public record DispozitieDetaliiDto(
    int Id,
    int? Numar,
    int? AnNumerotare,
    TipDispozitie TipDispozitie,
    string Titlu,
    string? Continut,
    DateTime DataEmitere,
    DateOnly? DataIntrareInVigoare,
    StatusActRedactional Status,
    bool EstePublicat,
    DateOnly? DataPublicareMol,
    bool AIntratInCircuit,
    bool EsteSemnat,
    string? NumeFisierSemnat,
    long? MarimeSemnat,
    DateTime? DataIncarcareSemnat,
    bool ContrasemnaturaRefuzata,
    string? ObiectieLegalitateSecretar,
    DateTime? DataRefuzContrasemnare,
    DateTime? DataInvalidare,
    MotivInvalidare? MotivInvalidare,
    string? MotivInvalidareEticheta,
    string? RefInvalidare,
    string? MotivInvalidareAltulText,
    int InstitutieId,
    DateTime CreatLa,
    List<SemnatarDispozitieDto> Semnatari);

public record SemnatarDispozitieDto(
    int Id,
    RolSemnatarDispozitie RolSemnatar,
    int? PersoanaId,
    int? ConsilierId,
    string Nume,
    DateOnly DataSemnare,
    int OrdineAfisare);
