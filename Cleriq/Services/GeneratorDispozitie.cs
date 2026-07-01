using System.Globalization;
using System.Text;
using Cleriq.Helpers;
using Cleriq.Models;

namespace Cleriq.Services;

// Generator de conținut pentru dispoziția primarului. Paralel cu GeneratorHcl, dar type-aware:
// act unilateral al primarului (art. 196 alin. (1) lit. b) — antet „PRIMARUL …", temei art. 155 +
// 196, formula „DISPUNE:", FĂRĂ vot/cvorum. Semnăturile (Emitent + Contrasemnătură secretar) stau
// în markdown (PDF-ul adaugă doar watermark — Pas 11). Antetul/titlul/semnăturile sunt comune;
// diferă doar corpul (dispozitiv generic vs. convocare cu ordine de zi — Pas 12).
public class GeneratorDispozitie : IGeneratorDispozitie
{
    private static readonly CultureInfo CulturaRo = new("ro-RO");

    public string GenereazaContinut(Dispozitie dispozitie)
        => GenereazaDocument(dispozitie, AppendCorpStandard);

    // Dispoziția de convocare (Pas 12): corp cu temei art. 133/134 + proiectul ordinii de zi din
    // ședință. Restul documentului (antet/titlu/semnături) e identic cu o dispoziție obișnuită.
    public string GenereazaConvocare(Dispozitie dispozitie, Sedinta sedinta)
        => GenereazaDocument(dispozitie, (sb, inst) => AppendCorpConvocare(sb, sedinta, inst));

    private static string GenereazaDocument(Dispozitie dispozitie, Action<StringBuilder, Institutie> appendCorp)
    {
        if (dispozitie.Institutie is null)
            throw new InvalidOperationException(
                "Dispozitie trebuie să aibă navigarea Institutie populată.");

        var institutie = dispozitie.Institutie;
        var sb = new StringBuilder();

        AppendBanerInvalidare(sb, dispozitie, institutie);
        AppendAntet(sb, institutie);
        AppendTitlu(sb, dispozitie, institutie);
        appendCorp(sb, institutie);
        AppendSemnaturi(sb, dispozitie);

        return sb.ToString();
    }

    // Baner pentru dispoziție invalidată (paritar HCL, dar cu formulare proprie dispoziției)
    private static void AppendBanerInvalidare(StringBuilder sb, Dispozitie dispozitie, Institutie institutie)
    {
        if (!dispozitie.DataInvalidare.HasValue)
            return;

        var motivText = dispozitie.MotivInvalidare switch
        {
            MotivInvalidare.AnulatInstanta => "anulată de instanță",
            MotivInvalidare.AbrogatHclUlterior => "abrogată prin dispoziție ulterioară",
            MotivInvalidare.Retractat => "revocată de primar (emitent)",
            MotivInvalidare.Caduc => "constatată caducă",
            MotivInvalidare.Inexistent => "constatată inexistentă",
            MotivInvalidare.Altul when !string.IsNullOrWhiteSpace(dispozitie.MotivInvalidareAltulText)
                => $"invalidată ({dispozitie.MotivInvalidareAltulText.Trim()})",
            _ => "invalidată"
        };
        var dataInvLocal = dispozitie.DataInvalidare.Value.LaFusOrar(institutie.FusOrar);
        sb.AppendLine("> **⚠️ ATENȚIE: ACT INVALIDAT**");
        sb.AppendLine(">");
        sb.AppendLine($"> Această dispoziție a fost {motivText} la data de {dataInvLocal:dd.MM.yyyy}.");
        if (!string.IsNullOrWhiteSpace(dispozitie.RefInvalidare))
            sb.AppendLine($"> Referință: {dispozitie.RefInvalidare}");
        sb.AppendLine();
    }

    // Antet oficial — autoritatea executivă (primarul), nu consiliul local
    private static void AppendAntet(StringBuilder sb, Institutie institutie)
    {
        sb.AppendLine("**ROMÂNIA**");
        sb.AppendLine($"**JUDEȚUL {institutie.Judet.ToUpper(CulturaRo)}**");
        sb.AppendLine($"**{institutie.Denumire.ToUpper(CulturaRo)}**");
        sb.AppendLine("**PRIMARUL**");
        sb.AppendLine();
    }

    private static void AppendTitlu(StringBuilder sb, Dispozitie dispozitie, Institutie institutie)
    {
        var numarText = dispozitie.Numar.HasValue && dispozitie.AnNumerotare.HasValue
            ? $"{dispozitie.Numar.Value}/{dispozitie.AnNumerotare.Value}"
            : PlaceholderAct.NumarNeatribuit;

        var dataEmitereLocala = dispozitie.DataEmitere.LaFusOrar(institutie.FusOrar);
        var dataEmitereText = dataEmitereLocala.ToString("dd MMMM yyyy", CulturaRo);

        sb.AppendLine($"# DISPOZIȚIA Nr. {numarText}");
        sb.AppendLine();
        sb.AppendLine($"**din {dataEmitereText}**");
        sb.AppendLine();
        sb.AppendLine($"**privind {dispozitie.Titlu}**");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
    }

    // Corp standard: preambul cu competența primarului (fără vot/cvorum) + dispozitiv-placeholder.
    private static void AppendCorpStandard(StringBuilder sb, Institutie institutie)
    {
        sb.AppendLine($"Primarul {institutie.Denumire},");
        sb.AppendLine();
        sb.AppendLine("**Având în vedere** atribuțiile ce îi revin potrivit legii și actele de fundamentare aferente,");
        sb.AppendLine();
        sb.AppendLine("**În temeiul prevederilor** art. 155 și ale art. 196 alin. (1) lit. b) din Ordonanța de urgență a Guvernului nr. 57/2019 privind Codul administrativ,");
        sb.AppendLine();

        sb.AppendLine("## DISPUNE:");
        sb.AppendLine();
        sb.AppendLine("**Art. 1.** _[Se completează aici dispozitivul.]_");
        sb.AppendLine();
    }

    // Corp de convocare: temei art. 133/134 + Art. 1 (convocare) + Art. 2 (proiectul ordinii de zi).
    private static void AppendCorpConvocare(StringBuilder sb, Sedinta sedinta, Institutie institutie)
    {
        var dataOraLocala = sedinta.DataOra.LaFusOrar(institutie.FusOrar);
        var dataOraText = dataOraLocala.ToString("dddd, d MMMM yyyy, ora HH:mm", CulturaRo);
        var tipText = sedinta.Tip.Eticheta().ToLower(CulturaRo);
        var modText = sedinta.ModDesfasurare.Eticheta().ToLower(CulturaRo);

        sb.AppendLine($"Primarul {institutie.Denumire},");
        sb.AppendLine();
        sb.AppendLine("**Având în vedere** necesitatea întrunirii Consiliului Local și proiectul ordinii de zi,");
        sb.AppendLine();
        sb.AppendLine("**În temeiul prevederilor** art. 133 alin. (1) și alin. (2), art. 134 alin. (1) lit. a) și ale art. 196 alin. (1) lit. b) din Ordonanța de urgență a Guvernului nr. 57/2019 privind Codul administrativ,");
        sb.AppendLine();

        sb.AppendLine("## DISPUNE:");
        sb.AppendLine();

        var locText = !string.IsNullOrWhiteSpace(sedinta.Loc) ? $", la {sedinta.Loc.Trim()}" : "";
        sb.AppendLine($"**Art. 1.** Se convoacă Consiliul Local al {institutie.Denumire} în ședință {tipText}, în data de {dataOraText}{locText} ({modText}).");
        sb.AppendLine();

        sb.AppendLine("**Art. 2.** Proiectul ordinii de zi:");
        sb.AppendLine();
        var puncte = sedinta.Puncte.OrderBy(p => p.Ordine).ToList();
        if (puncte.Count == 0)
        {
            sb.AppendLine("_(Ordinea de zi nu a fost încă stabilită.)_");
        }
        else
        {
            var index = 1;
            foreach (var p in puncte)
            {
                sb.AppendLine($"{index}. {p.Titlu} _({p.Tip.Eticheta()})_");
                index++;
            }
        }
        sb.AppendLine();
    }

    // Semnături — Emitent (Primar / p. Primar la înlocuitor) + Contrasemnătură secretar general.
    private static void AppendSemnaturi(StringBuilder sb, Dispozitie dispozitie)
    {
        sb.AppendLine("---");
        sb.AppendLine();

        var semnatari = dispozitie.Semnatari.OrderBy(s => s.OrdineAfisare).ToList();
        var emitent = semnatari.FirstOrDefault(s => s.RolSemnatar == RolSemnatarDispozitie.Emitent);
        var secretar = semnatari.FirstOrDefault(s => s.RolSemnatar == RolSemnatarDispozitie.SecretarContrasemnatura);

        if (emitent != null)
        {
            // Înlocuitor de drept (viceprimar) = emitent pe Consilier → prefix „p. PRIMAR"
            var esteInlocuitor = emitent.ConsilierId != null;
            sb.AppendLine(esteInlocuitor ? "**p. PRIMAR,**" : "**PRIMAR,**");
            sb.AppendLine();
            sb.AppendLine(NumeSemnatar(emitent));
            if (esteInlocuitor)
                sb.AppendLine("_Viceprimar_");
            sb.AppendLine();
        }

        if (dispozitie.ContrasemnaturaRefuzata)
        {
            // Primarul a emis pe răspundere proprie peste obiecția de legalitate a secretarului (art. 197 alin. (3)).
            sb.AppendLine("**Contrasemnătura secretarului general — REFUZATĂ (obiecție de legalitate).**");
            if (!string.IsNullOrWhiteSpace(dispozitie.ObiectieLegalitateSecretar))
                sb.AppendLine($"_Obiecție: {dispozitie.ObiectieLegalitateSecretar.Trim()}_");
            sb.AppendLine();
        }
        else if (secretar != null)
        {
            sb.AppendLine("**Contrasemnează pentru legalitate,**");
            sb.AppendLine("**SECRETAR GENERAL,**");
            sb.AppendLine();
            sb.AppendLine(NumeSemnatar(secretar));
            sb.AppendLine();
        }
    }

    private static string NumeSemnatar(SemnatarDispozitie s)
    {
        if (s.Persoana != null) return s.Persoana.NumeComplet;
        if (s.Consilier != null) return s.Consilier.NumeComplet;
        return "_[nume nedisponibil]_";
    }
}
