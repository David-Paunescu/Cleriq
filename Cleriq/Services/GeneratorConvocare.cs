using System.Globalization;
using System.Net;
using System.Text;
using Cleriq.Helpers;
using Cleriq.Models;

namespace Cleriq.Services;

public class GeneratorConvocare : IGeneratorConvocare
{
    private static readonly CultureInfo CulturaRo = new("ro-RO");

    public ContinutConvocare Genereaza(Sedinta sedinta, Consilier consilier)
    {
        var subiect = GenereazaSubiect(sedinta);
        var emailHtml = GenereazaEmail(sedinta, consilier);
        var smsText = GenereazaSms(sedinta);
        return new ContinutConvocare(subiect, emailHtml, smsText);
    }

    private static string GenereazaSubiect(Sedinta s)
    {
        var dataStr = s.DataOra.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        return $"Convocare ședință {s.Tip.Eticheta().ToLower()} — {s.Institutie.Denumire} — {dataStr}";
    }

    private static string GenereazaEmail(Sedinta s, Consilier c)
    {
        var dataOraStr = s.DataOra.ToString("dddd, d MMMM yyyy, HH:mm", CulturaRo);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"ro\">");
        sb.AppendLine("<head><meta charset=\"UTF-8\"></head>");
        sb.AppendLine("<body style=\"font-family: Arial, sans-serif; color: #222; max-width: 720px;\">");
        sb.AppendLine("<h2>Convocare ședință Consiliu Local</h2>");
        sb.AppendLine($"<p><strong>{HtmlEnc(s.Institutie.Denumire)}</strong></p>");
        sb.AppendLine($"<p>Stimată/Stimate doamnă/domnule consilier <strong>{HtmlEnc(c.NumeComplet)}</strong>,</p>");
        sb.AppendLine($"<p>Sunteți convocat(ă) să participați la ședința {HtmlEnc(s.Tip.Eticheta().ToLower())} a Consiliului Local.</p>");

        sb.AppendLine("<h3>Detalii ședință</h3>");
        sb.AppendLine("<ul>");
        sb.AppendLine($"<li><strong>Data și ora:</strong> {dataOraStr} UTC</li>");
        if (!string.IsNullOrWhiteSpace(s.Loc))
            sb.AppendLine($"<li><strong>Loc:</strong> {HtmlEnc(s.Loc)}</li>");
        sb.AppendLine($"<li><strong>Tip ședință:</strong> {HtmlEnc(s.Tip.Eticheta())}</li>");
        sb.AppendLine($"<li><strong>Mod desfășurare:</strong> {HtmlEnc(s.ModDesfasurare.Eticheta())}</li>");
        if (!string.IsNullOrWhiteSpace(s.Numar))
            sb.AppendLine($"<li><strong>Număr ședință:</strong> {HtmlEnc(s.Numar)}</li>");
        sb.AppendLine("</ul>");

        sb.AppendLine("<h3>Ordinea de zi</h3>");
        if (!s.Puncte.Any())
        {
            sb.AppendLine("<p><em>(Ordinea de zi nu a fost încă stabilită)</em></p>");
        }
        else
        {
            sb.AppendLine("<ol>");
            foreach (var p in s.Puncte.OrderBy(p => p.Ordine))
            {
                sb.AppendLine($"<li>{HtmlEnc(p.Titlu)} <em>({HtmlEnc(p.Tip.Eticheta())})</em></li>");
            }
            sb.AppendLine("</ol>");
        }

        sb.AppendLine("<p style=\"margin-top: 24px;\">Vă mulțumim,<br/>Secretariatul Consiliului Local</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string GenereazaSms(Sedinta s)
    {
        var dataStr = s.DataOra.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        var locStr = !string.IsNullOrWhiteSpace(s.Loc) ? $", {s.Loc}" : "";
        var nrPuncte = s.Puncte.Count;
        return $"Convocare CL {s.Institutie.Denumire}: ședință {s.Tip.Eticheta().ToLower()} {dataStr}{locStr}. {nrPuncte} puncte pe ordinea de zi. Detalii pe email.";
    }

    private static string HtmlEnc(string s) => WebUtility.HtmlEncode(s);
}