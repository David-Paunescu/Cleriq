using Cleriq.DTOs;
using Cleriq.Models;

namespace Cleriq.Helpers;

public static class ExtensiiConvocare
{
    /// <summary>
    /// Calculează status-ul general al unei convocări din statusurile per canal (email + SMS).
    /// Folosit identic pe rutele interne (ConvocareController) și pe portalul public.
    /// </summary>
    public static StatusConvocare StatusGeneral(this Convocare co)
    {
        // Canal "activ" = canal cu destinație fizică (status setat și diferit de FaraDestinatie).
        // null = tranzitoriu (cazul de mijloc în controller); FaraDestinatie = consilier fără coordonate.
        var statusuriActive = new[] { co.EmailStatus, co.SmsStatus }
            .Where(s => s.HasValue && s.Value != StatusTrimitere.FaraDestinatie)
            .Select(s => s!.Value)
            .ToList();

        if (statusuriActive.Count == 0)
            return StatusConvocare.FaraCoordonate;

        if (statusuriActive.Contains(StatusTrimitere.InAsteptare))
            return StatusConvocare.InCursDeTrimitere;

        // Toate canalele active sunt fie Trimisa, fie Esuata
        var reusite = statusuriActive.Count(s => s == StatusTrimitere.Trimisa);
        if (reusite == statusuriActive.Count) return StatusConvocare.TotalSucces;
        if (reusite == 0) return StatusConvocare.Esuata;
        return StatusConvocare.PartialSucces;
    }
}