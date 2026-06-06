namespace Cleriq.DTOs;

public enum CategorieOrfan
{
    FaraRandInDb = 1,        
    SoftDeletedVechi = 2
}

public record FisierOrfanDto(
    string Cheie,
    long Marime,
    DateTime DataModificare,
    CategorieOrfan Categorie,
    DateTime? StersLa,
    int? InstitutieId);
public record RaportOrfaniDto(
    int TotalFisiere,
    long TotalBytes,
    int CategoriaFaraRandInDb,
    int CategoriaSoftDeletedVechi,
    int ZilePrag,
    List<FisierOrfanDto> Fisiere);

public record RezultatStergereDto(
    int TotalCandidati,
    int Sterse,
    int Esuate,
    long BytesEliberati,
    int SterseFaraRandInDb,
    int SterseSoftDeletedVechi,
    int ZilePrag,
    List<FisierOrfanDto> FisiereEsuate);