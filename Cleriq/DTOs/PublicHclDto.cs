using Cleriq.Models;

namespace Cleriq.DTOs;

public record PublicHclDto(
    int Id,
    int? Numar,
    int? AnNumerotare,
    TipHcl TipHcl,
    string Titlu,
    DateTime DataAdoptare,
    DateOnly? DataIntrareInVigoare,
    StatusHclRedactional Status,
    bool EsteInvalidat,
    DateOnly? DataPublicareMol);

public record PublicHclDetaliiDto(
    int Id,
    int? Numar,
    int? AnNumerotare,
    TipHcl TipHcl,
    string Titlu,
    string? Continut,
    DateTime DataAdoptare,
    DateOnly? DataIntrareInVigoare,
    StatusHclRedactional Status,
    int VotPentru,
    int VotImpotriva,
    int VotAbtinere,
    TipMajoritate TipMajoritate,
    bool EsteSemnat,
    DateOnly? DataPublicareMol,
    string? MotivLipsaSemnaturaPresedinte,
    MotivInvalidare? MotivInvalidare,
    string? RefInvalidare,
    DateTime? DataInvalidare,
    List<PublicSemnatarHclDto> Semnatari,
    List<PublicDocumentHclDto> Anexe,
    List<PublicRelatieHclDto> RelatiiSursa,
    List<PublicRelatieHclDto> RelatiiTinta);

public record PublicSemnatarHclDto(
    RolSemnatar RolSemnatar,
    string NumeComplet,
    int OrdineAfisare);

public record PublicDocumentHclDto(
    int Id,
    string Denumire,
    string? Descriere,
    TipDocumentHcl? TipDocumentHcl,
    int? NumarOrdinAnexa,
    string NumeFisierOriginal,
    long Marime,
    int Ordine);

// Capătul „celălalt" al relației (ținta la RelatiiSursa, sursa la RelatiiTinta).
// ActNepublicat = ținta internă încă nu e adoptată (Status < Numerotat): degradăm la marcaj.
public record PublicRelatieHclDto(
    TipRelatieHcl TipRelatie,
    int? HclId,
    string? NumarFormatat,
    string? Titlu,
    string? ReferintaActExternText,
    bool ActNepublicat);