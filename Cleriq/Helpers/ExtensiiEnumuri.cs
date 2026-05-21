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

    public static string Eticheta(this RezultatPunct? r) => r switch
    {
        RezultatPunct.Adoptat => "Adoptat",
        RezultatPunct.Respins => "Respins",
        RezultatPunct.Amanat => "Amânat",
        RezultatPunct.Retras => "Retras",
        null => "În curs (vot deschis)",
        _ => r.ToString()!
    };
}