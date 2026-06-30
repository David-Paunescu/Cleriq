using Cleriq.Models;

namespace Cleriq.DTOs;

// === Body-uri pentru endpoint-urile HclController ===

public record CreareHclDto(int PunctOrdineZiId, TipHcl TipHcl);

public record EditareContinutHclDto(string Continut);

public record AtribuireNumarHclDto(int Numar, bool ConfirmaCuLacune);

public record InvalidareHclDto(
    MotivInvalidare Motiv,
    string? MotivAltulText,
    string? RefInvalidare,
    bool ConfirmaCuRelatiiActive);

public record PublicareHclDto(bool EstePublicat);

public record PublicareMolDto(DateOnly DataPublicareMol);

public record AnulareMolDto(string Motiv);

// === Răspunsuri ===

public record HclDto(
    int Id,
    int? Numar,
    int? AnNumerotare,
    TipHcl TipHcl,
    string Titlu,
    DateTime DataAdoptare,
    DateOnly? DataIntrareInVigoare,
    StatusActRedactional Status,
    bool EstePublicat,
    DateOnly? DataPublicareMol,
    DateTime? DataInvalidare,
    MotivInvalidare? MotivInvalidare,
    int InstitutieId,
    DateTime CreatLa);

public record HclDetaliiDto(
    int Id,
    int? Numar,
    int? AnNumerotare,
    TipHcl TipHcl,
    string Titlu,
    string? Continut,
    DateTime DataAdoptare,
    DateOnly? DataIntrareInVigoare,
    StatusActRedactional Status,
    int PunctOrdineZiId,
    int VotPentru,
    int VotImpotriva,
    int VotAbtinere,
    TipMajoritate TipMajoritate,
    bool EstePublicat,
    DateOnly? DataPublicareMol,
    bool AIntratInCircuit,
    bool EsteSemnat,
    string? NumeFisierSemnat,
    long? MarimeSemnat,
    DateTime? DataIncarcareSemnat,
    string? MotivLipsaSemnaturaPresedinte,
    DateTime? DataInvalidare,
    MotivInvalidare? MotivInvalidare,
    string? RefInvalidare,
    string? MotivInvalidareAltulText,
    int InstitutieId,
    DateTime CreatLa,
    List<SemnatarHclDto> Semnatari,
    List<DocumentHclDto> Documente,
    List<RelatieHclDto> RelatiiSursa,
    List<RelatieHclDto> RelatiiTinta,
    List<ComunicareHclPrefectDto> Comunicari);

public record DocumentHclDto(
    int Id,
    string Denumire,
    string? Descriere,
    TipDocumentHcl? TipDocumentHcl,
    int? NumarOrdinAnexa,
    string NumeFisierOriginal,
    long Marime,
    int Ordine);

public record MotivLipsaPresedinteDto(string Motiv);

public record SugestieNumarDto(int Numar, int An);