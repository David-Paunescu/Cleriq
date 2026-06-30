using Cleriq.Models;

namespace Cleriq.Helpers;

public static class ExtensiiEnumuri
{
    public static string Eticheta(this TipSedinta t) => t switch
    {
        TipSedinta.Ordinara => "Ordinară",
        TipSedinta.Extraordinara => "Extraordinară",
        TipSedinta.DeIndata => "De îndată",
        _ => t.ToString()
    };

    public static string Eticheta(this ModDesfasurare m) => m switch
    {
        ModDesfasurare.Fizic => "Fizic",
        ModDesfasurare.Online => "Online",
        ModDesfasurare.Hibrid => "Hibrid",
        _ => m.ToString()
    };

    public static string Eticheta(this StatusPrezenta s) => s switch
    {
        StatusPrezenta.Prezent => "Prezent",
        StatusPrezenta.Absent => "Absent",
        StatusPrezenta.AbsentMotivat => "Absent motivat",
        StatusPrezenta.OnlinePrezent => "Online prezent",
        _ => s.ToString()
    };

    public static string Eticheta(this TipPunct t) => t switch
    {
        TipPunct.ProiectHCL => "Proiect HCL",
        TipPunct.Informare => "Informare",
        TipPunct.Diverse => "Diverse",
        _ => t.ToString()
    };

    public static string Eticheta(this TipMajoritate? t) => t switch
    {
        TipMajoritate.Simpla => "Simplă",
        TipMajoritate.Absoluta => "Absolută",
        TipMajoritate.Calificata => "Calificată",
        null => "—",
        _ => t.ToString()!
    };

    public static string Eticheta(this TipMajoritate t) => ((TipMajoritate?)t).Eticheta();

    public static string Eticheta(this RezultatPunct? r) => r switch
    {
        RezultatPunct.Adoptat => "Adoptat",
        RezultatPunct.Respins => "Respins",
        RezultatPunct.Amanat => "Amânat",
        RezultatPunct.Retras => "Retras",
        null => "În curs (vot deschis)",
        _ => r.ToString()!
    };

    public static string Eticheta(this TipDocument t) => t switch
    {
        TipDocument.ProiectHCL => "Proiect HCL",
        TipDocument.ExpunereDeMotive => "Expunere de motive",
        TipDocument.Aviz => "Aviz",
        TipDocument.Raport => "Raport",
        TipDocument.Anexa => "Anexă",
        TipDocument.Altele => "Alt document",
        _ => t.ToString()
    };

    public static string Eticheta(this TipFunctie t) => t switch
    {
        TipFunctie.Primar => "Primar",
        TipFunctie.Viceprimar => "Viceprimar",
        TipFunctie.SecretarUat => "Secretar UAT",
        _ => t.ToString()
    };

    public static string Eticheta(this StatusHclRedactional s) => s switch
    {
        StatusHclRedactional.Draft => "Draft",
        StatusHclRedactional.Numerotat => "Numerotat",
        StatusHclRedactional.Semnat => "Semnat",
        _ => s.ToString()
    };

    public static string Eticheta(this TipHcl t) => t switch
    {
        TipHcl.Normativ => "Normativ",
        TipHcl.Individual => "Individual",
        _ => t.ToString()
    };

    public static string Eticheta(this TipRelatieHcl t) => t switch
    {
        TipRelatieHcl.Modifica => "Modifică",
        TipRelatieHcl.Abroga => "Abrogă",
        TipRelatieHcl.Suspenda => "Suspendă",
        TipRelatieHcl.PuneInAplicare => "Pune în aplicare",
        TipRelatieHcl.Completeaza => "Completează",
        TipRelatieHcl.Republica => "Republică",
        _ => t.ToString()
    };

    public static string Eticheta(this CanalTransmiterePrefect c) => c switch
    {
        CanalTransmiterePrefect.Posta => "Poștă",
        CanalTransmiterePrefect.EmailOficial => "Email oficial",
        CanalTransmiterePrefect.Curier => "Curier",
        CanalTransmiterePrefect.Prezentare => "Prezentare directă",
        CanalTransmiterePrefect.ePoartal => "ePoartal",
        CanalTransmiterePrefect.Altul => "Altul",
        _ => c.ToString()
    };

    public static string Eticheta(this RolSemnatar r) => r switch
    {
        RolSemnatar.PresedinteSedinta => "Președinte de ședință",
        RolSemnatar.SecretarUat => "Secretar UAT",
        RolSemnatar.SemnatarAlternativArt140 => "Semnatar alternativ (art. 140 alin. 2)",
        _ => r.ToString()
    };

    public static string Eticheta(this MotivInvalidare? m) => m switch
    {
        MotivInvalidare.AnulatInstanta => "Anulat de instanță",
        MotivInvalidare.AbrogatHclUlterior => "Abrogat prin HCL ulterior",
        MotivInvalidare.Retractat => "Retractat",
        MotivInvalidare.Caduc => "Caducitate (expirarea termenului / dispariția obiectului)",
        MotivInvalidare.Inexistent => "Inexistent (nepublicat în MOL / lipsă element esențial)",
        MotivInvalidare.Altul => "Altul",
        null => "—",
        _ => m.ToString()!
    };

    public static string Eticheta(this TipDocumentHcl? t) => t switch
    {
        TipDocumentHcl.Anexa => "Anexă",
        TipDocumentHcl.RaportSpecialitate => "Raport de specialitate",
        TipDocumentHcl.ExpunereDeMotive => "Expunere de motive",
        TipDocumentHcl.AvizComisie => "Aviz comisie",
        TipDocumentHcl.Justificativ => "Justificativ",
        TipDocumentHcl.Altul => "Altul",
        null => "—",
        _ => t.ToString()!
    };

    public static string Eticheta(this RaspunsPrefect? r) => r switch
    {
        RaspunsPrefect.Acceptat => "Acceptat",
        RaspunsPrefect.RespinsLegalitate => "Respins (legalitate)",
        RaspunsPrefect.CereClarificari => "Cere clarificări",
        RaspunsPrefect.FaraRaspuns => "Fără răspuns",
        null => "—",
        _ => r.ToString()!
    };
}