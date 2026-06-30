using System.Globalization;
using System.Text;
using Cleriq.Helpers;
using Cleriq.Models;

namespace Cleriq.Services;

public class GeneratorHcl : IGeneratorHcl
{
    private static readonly CultureInfo CulturaRo = new("ro-RO");

    public string GenereazaContinut(Hcl hcl)
    {
        if (hcl.PunctOrdineZi?.Sedinta?.Institutie is null)
            throw new InvalidOperationException(
                "Hcl trebuie să aibă navigările PunctOrdineZi.Sedinta.Institutie populate.");

        var sedinta = hcl.PunctOrdineZi.Sedinta;
        var institutie = sedinta.Institutie;
        var sb = new StringBuilder();

        // Baner pentru HCL invalidat (subdecizia S49 — generatorul anunță explicit)
        if (hcl.DataInvalidare.HasValue)
        {
            var motivText = hcl.MotivInvalidare switch
            {
                MotivInvalidare.AnulatInstanta => "anulată de instanță",
                MotivInvalidare.AbrogatHclUlterior => "abrogată prin hotărâre ulterioară",
                MotivInvalidare.Retractat => "retractată",
                MotivInvalidare.Caduc => "constatată caducă",
                MotivInvalidare.Inexistent => "constatată inexistentă",
                MotivInvalidare.Altul when !string.IsNullOrWhiteSpace(hcl.MotivInvalidareAltulText)
                    => $"invalidată ({hcl.MotivInvalidareAltulText.Trim()})",
                _ => "invalidată"
            };
            var dataInvLocal = hcl.DataInvalidare.Value.LaFusOrar(institutie.FusOrar);
            sb.AppendLine("> **⚠️ ATENȚIE: ACT INVALIDAT**");
            sb.AppendLine(">");
            sb.AppendLine($"> Această hotărâre a fost {motivText} la data de {dataInvLocal:dd.MM.yyyy}.");
            if (!string.IsNullOrWhiteSpace(hcl.RefInvalidare))
                sb.AppendLine($"> Referință: {hcl.RefInvalidare}");
            sb.AppendLine();
        }

        // Antet oficial
        sb.AppendLine("**ROMÂNIA**");
        sb.AppendLine($"**JUDEȚUL {institutie.Judet.ToUpper(CulturaRo)}**");
        sb.AppendLine($"**{institutie.Denumire.ToUpper(CulturaRo)}**");
        sb.AppendLine("**CONSILIUL LOCAL**");
        sb.AppendLine();

        var numarText = hcl.Numar.HasValue && hcl.AnNumerotare.HasValue
                    ? $"{hcl.Numar.Value}/{hcl.AnNumerotare.Value}"
                    : PlaceholderHcl.NumarNeatribuit;

        var dataAdoptareLocala = hcl.DataAdoptare.LaFusOrar(institutie.FusOrar);
        var dataAdoptareText = dataAdoptareLocala.ToString("dd MMMM yyyy", CulturaRo);

        sb.AppendLine($"# HOTĂRÂREA Nr. {numarText}");
        sb.AppendLine();
        sb.AppendLine($"**din {dataAdoptareText}**");
        sb.AppendLine();
        sb.AppendLine($"**privind {hcl.Titlu}**");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Preambul
        var dataSedintaLocala = sedinta.DataOra.LaFusOrar(institutie.FusOrar);
        var dataSedintaText = dataSedintaLocala.ToString("dd MMMM yyyy", CulturaRo);

        sb.AppendLine($"Consiliul Local al {institutie.Denumire}, întrunit în ședință {sedinta.Tip.Eticheta().ToLower(CulturaRo)} din data de {dataSedintaText},");
        sb.AppendLine();

        sb.AppendLine("**Având în vedere:**");
        sb.AppendLine();
        sb.AppendLine($"- Proiectul de hotărâre înregistrat la pct. {hcl.PunctOrdineZi.Ordine} de pe ordinea de zi a ședinței");

        var documenteAuxiliare = hcl.Documente
            .Where(d => d.TipDocumentHcl.HasValue && d.TipDocumentHcl.Value != Models.TipDocumentHcl.Anexa)
            .OrderBy(d => d.Ordine)
            .ToList();
        foreach (var doc in documenteAuxiliare)
        {
            sb.AppendLine($"- {doc.TipDocumentHcl.Eticheta()}: {doc.Denumire}");
        }
        sb.AppendLine();

        sb.AppendLine("**Luând în considerare** prevederile art. 129 și art. 139 din Ordonanța de urgență a Guvernului nr. 57/2019 privind Codul administrativ,");
        sb.AppendLine();
        sb.AppendLine("**În temeiul prevederilor** art. 196 alin. (1) lit. a) din OUG nr. 57/2019,");
        sb.AppendLine();

        // Vot snapshot — imutabil după generare
        var majoritateText = hcl.TipMajoritate.Eticheta().ToLower(CulturaRo);
        sb.AppendLine($"Cu un număr de **{hcl.VotPentru}** voturi PENTRU, **{hcl.VotImpotriva}** voturi ÎMPOTRIVĂ și **{hcl.VotAbtinere}** ABȚINERI, fiind îndeplinită condiția majorității **{majoritateText}**,");
        sb.AppendLine();

        // Dispozitivul — placeholder pentru secretar
        sb.AppendLine("## HOTĂRĂȘTE:");
        sb.AppendLine();
        sb.AppendLine("**Art. 1.** _[Secretarul completează aici dispozitivul hotărârii.]_");
        sb.AppendLine();

        // Anexe (ordonate după NumarOrdinAnexa)
        var anexe = hcl.Documente
            .Where(d => d.TipDocumentHcl == Models.TipDocumentHcl.Anexa)
            .OrderBy(d => d.NumarOrdinAnexa ?? 0)
            .ToList();
        if (anexe.Any())
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("**Anexele care fac parte integrantă din prezenta hotărâre:**");
            sb.AppendLine();
            foreach (var anexa in anexe)
            {
                sb.AppendLine($"- **Anexa nr. {anexa.NumarOrdinAnexa}** — {anexa.Denumire}");
            }
            sb.AppendLine();
        }

        // Relații cu alte HCL sau acte externe
        if (hcl.RelatiiSursa.Any())
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("**Această hotărâre produce următoarele efecte juridice:**");
            sb.AppendLine();
            foreach (var rel in hcl.RelatiiSursa.OrderBy(r => r.TipRelatie))
            {
                var actiune = rel.TipRelatie.Eticheta();
                if (rel.HclTinta != null)
                {
                    var nrTinta = rel.HclTinta.Numar.HasValue && rel.HclTinta.AnNumerotare.HasValue
                        ? $"HCL nr. {rel.HclTinta.Numar.Value}/{rel.HclTinta.AnNumerotare.Value}"
                        : "HCL (fără număr)";
                    sb.AppendLine($"- **{actiune}** {nrTinta} — {rel.HclTinta.Titlu}");
                }
                else if (!string.IsNullOrWhiteSpace(rel.ReferintaActExternText))
                {
                    sb.AppendLine($"- **{actiune}** {rel.ReferintaActExternText}");
                }
            }
            sb.AppendLine();
        }

        // Semnături — Președinte ședință SAU Semnatari alternativi (art. 140 alin. 2), apoi Secretar UAT
        sb.AppendLine("---");
        sb.AppendLine();

        var semnatari = hcl.Semnatari.OrderBy(s => s.OrdineAfisare).ToList();
        var presedinte = semnatari.FirstOrDefault(s => s.RolSemnatar == RolSemnatar.PresedinteSedinta);
        var alternativi = semnatari.Where(s => s.RolSemnatar == RolSemnatar.SemnatarAlternativArt140).ToList();
        var secretarUat = semnatari.FirstOrDefault(s => s.RolSemnatar == RolSemnatar.SecretarUat);

        if (presedinte != null)
        {
            sb.AppendLine("**Președinte de ședință,**");
            sb.AppendLine();
            sb.AppendLine(NumeSemnatar(presedinte));
            sb.AppendLine();
        }
        else if (alternativi.Any())
        {
            sb.AppendLine("**Semnatari conform art. 140 alin. (2) din OUG nr. 57/2019**");
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(hcl.MotivLipsaSemnaturaPresedinte))
            {
                sb.AppendLine($"_Motiv lipsă semnătură președinte: {hcl.MotivLipsaSemnaturaPresedinte}_");
                sb.AppendLine();
            }
            foreach (var alt in alternativi.OrderBy(s => s.OrdineAfisare))
            {
                sb.AppendLine($"- {NumeSemnatar(alt)}");
            }
            sb.AppendLine();
        }

        if (secretarUat != null)
        {
            sb.AppendLine("**Contrasemnează,**");
            sb.AppendLine("**Secretar general al U.A.T.,**");
            sb.AppendLine();
            sb.AppendLine(NumeSemnatar(secretarUat));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string NumeSemnatar(SemnatarHcl s)
    {
        if (s.Persoana != null) return s.Persoana.NumeComplet;
        if (s.Consilier != null) return s.Consilier.NumeComplet;
        return "_[nume nedisponibil]_";
    }
}