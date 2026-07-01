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

// Publicare pe portal (EstePublicat), reversibilă — portalul public e Faza 9, aici ținem doar starea
// internă. Individualele sunt nepublice implicit: publicarea lor e un override deliberat
// (ConfirmaPublicareIndividuala=true + Motiv, gatat în controller); câmpurile sunt ignorate la Normativ.
public record PublicareDispozitieDto(
    bool EstePublicat,
    bool ConfirmaPublicareIndividuala = false,
    string? Motiv = null);

// Publicare în MOL (aducere la cunoștință publică, art. 198) — setează latch-ul AIntratInCircuit.
// Aceleași câmpuri de override pentru individuale ca la publicarea pe portal.
public record PublicareMolDispozitieDto(
    DateOnly DataPublicareMol,
    bool ConfirmaPublicareIndividuala = false,
    string? Motiv = null);

// „Anulează MOL" = corecție de metadată (motiv obligatoriu + audit); NU resetează latch-ul.
public record AnulareMolDispozitieDto(string Motiv);

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
    int? SedintaId,
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
    int? SedintaId,
    int InstitutieId,
    DateTime CreatLa,
    List<SemnatarDispozitieDto> Semnatari,
    List<ComunicareDispozitiePrefectDto> Comunicari);

public record SemnatarDispozitieDto(
    int Id,
    RolSemnatarDispozitie RolSemnatar,
    int? PersoanaId,
    int? ConsilierId,
    string Nume,
    DateOnly DataSemnare,
    int OrdineAfisare);
