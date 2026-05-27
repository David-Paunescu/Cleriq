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
        var emailIncercat = co.EmailStatus == StatusTrimitere.Trimisa
                         || co.EmailStatus == StatusTrimitere.Esuata;
        var smsIncercat = co.SmsStatus == StatusTrimitere.Trimisa
                       || co.SmsStatus == StatusTrimitere.Esuata;
        var emailReusit = co.EmailStatus == StatusTrimitere.Trimisa;
        var smsReusit = co.SmsStatus == StatusTrimitere.Trimisa;

        var totalIncercari = (emailIncercat ? 1 : 0) + (smsIncercat ? 1 : 0);
        var totalReusite = (emailReusit ? 1 : 0) + (smsReusit ? 1 : 0);

        if (totalIncercari == 0)
            return StatusConvocare.FaraCoordonate;
        if (totalReusite == totalIncercari)
            return StatusConvocare.TotalSucces;
        if (totalReusite == 0)
            return StatusConvocare.Esuata;
        return StatusConvocare.PartialSucces;
    }
}