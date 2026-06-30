using Cleriq.Models;
using Cleriq.Services;

namespace Cleriq.Helpers;

// File-handling comun pentru varianta semnată (a 3-a folosire: PV + HCL + Dispoziție).
// Captează doar partea identică — validare PDF, stocare+hash, replace, curățare. Gărzile de
// freeze (cine/când poate înlocui/șterge) diferă per act și rămân inline în fiecare controller.
public static class VariantaSemnata
{
    // Validare comună a fișierului PDF semnat. Întoarce mesajul de eroare (→ 400) sau null dacă e ok.
    public static string? ValideazaPdf(IFormFile? fisier, string etichetaAct)
    {
        if (fisier is null || fisier.Length == 0)
            return "Fișier lipsă.";
        if (fisier.Length > ValidareDocument.MarimeMaxima)
            return $"Fișierul depășește limita de {ValidareDocument.MarimeMaxima / (1024 * 1024)} MB.";

        var extensie = Path.GetExtension(fisier.FileName);
        if (!string.Equals(extensie, ".pdf", StringComparison.OrdinalIgnoreCase))
            return $"Doar fișiere PDF sunt acceptate pentru {etichetaAct}.";

        return null;
    }

    // Stochează fișierul și setează cele 5 câmpuri pe act. Întoarce calea VECHE (pentru ștergere
    // post-commit) — replace-ul efectiv îl face apelantul DUPĂ SaveChanges, cu StergeVecheAsync.
    public static async Task<string?> StocheazaAsync(
        IActCuVariantaSemnata act, IFormFile fisier, IStocareDocumente stocare,
        int institutieId, CancellationToken ct)
    {
        var caleVeche = act.CaleStocareSemnat;

        FisierStocat stocat;
        await using (var stream = fisier.OpenReadStream())
        {
            stocat = await stocare.SalveazaAsync(institutieId, fisier.FileName, stream, ct);
        }

        act.CaleStocareSemnat = stocat.Cheie;
        act.NumeFisierSemnat = Path.GetFileName(fisier.FileName);
        act.MarimeSemnat = stocat.Marime;
        act.HashSha256Semnat = stocat.HashSha256;
        act.DataIncarcareSemnat = DateTime.UtcNow;

        return caleVeche;
    }

    // Șterge fișierul vechi după un replace (best-effort; eșecul lasă un orfan acceptabil pentru
    // scanul de mentenanță). Apelat DUPĂ SaveChanges, ca în pattern-ul PV/HCL.
    public static async Task StergeVecheAsync(
        IStocareDocumente stocare, string? caleVeche, string caleNoua, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(caleVeche) || caleVeche == caleNoua)
            return;

        try { await stocare.StergeAsync(caleVeche, ct); }
        catch { /* orfan acceptabil; cleanup viitor prin Mentenanță */ }
    }

    // Golește metadatele variantei semnate (DELETE). Fișierul fizic rămâne (orfan FaraRandInDb,
    // măturat de mentenanță după grace period — păstrare forensică).
    public static void Curata(IActCuVariantaSemnata act)
    {
        act.CaleStocareSemnat = null;
        act.NumeFisierSemnat = null;
        act.MarimeSemnat = null;
        act.HashSha256Semnat = null;
        act.DataIncarcareSemnat = null;
    }
}
