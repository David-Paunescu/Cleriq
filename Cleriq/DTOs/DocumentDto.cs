using Cleriq.Models;

namespace Cleriq.DTOs;

public record DocumentDto(
    int Id,
    string Denumire,
    string? Descriere,
    TipDocument TipDocument,
    string NumeFisierOriginal,
    string TipMime,
    long Marime,
    string HashSha256,
    bool EstePublic,
    int Ordine,
    int? SedintaId,
    int? PunctId,
    DateTime CreatLa,
    int? HclId,
    TipDocumentHcl? TipDocumentHcl,
    int? NumarOrdinAnexa);

public record ActualizareDocumentDto(
    string Denumire,
    string? Descriere,
    TipDocument TipDocument,
    int Ordine,
    TipDocumentHcl? TipDocumentHcl,
    int? NumarOrdinAnexa);

public record SetareVizibilitateDto(bool EstePublic);