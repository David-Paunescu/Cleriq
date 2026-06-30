namespace Cleriq.Models;

// Contract comun pentru actele care stochează o variantă semnată extern (Nivel 1: scan PDF).
// Implementat de Hcl, ProcesVerbal, Dispozitie — câmpurile există deja identic pe toate trei
// (fără migrație). Permite extragerea file-handling-ului (validare/stocare/hash/replace) în
// Helpers/VariantaSemnata, fără duplicare per controller. Gărzile de freeze rămân inline.
public interface IActCuVariantaSemnata
{
    string? CaleStocareSemnat { get; set; }
    string? NumeFisierSemnat { get; set; }
    long? MarimeSemnat { get; set; }
    string? HashSha256Semnat { get; set; }
    DateTime? DataIncarcareSemnat { get; set; }
}
