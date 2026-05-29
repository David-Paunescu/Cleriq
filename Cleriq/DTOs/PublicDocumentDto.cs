using Cleriq.Models;

namespace Cleriq.DTOs;

public record PublicDocumentDto(
    int Id,
    string Denumire,
    string? Descriere,
    TipDocument TipDocument,
    string NumeFisierOriginal,
    string TipMime,
    long Marime,
    int Ordine);