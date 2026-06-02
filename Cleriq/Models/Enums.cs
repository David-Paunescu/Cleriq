namespace Cleriq.Models;

public enum TipInstitutie
{
    Oras = 1,
    Municipiu = 2,
    Comuna = 3
}

public enum StatusAbonament
{
    Activ = 1,
    Suspendat = 2,
    Anulat = 3
}

public enum RolComisie
{
    Presedinte = 1,
    Secretar = 2,
    Membru = 3
}

public enum TipSedinta
{
    Ordinara = 1,
    Extraordinara = 2,
    DeIndata = 3
}

public enum ModDesfasurare
{
    Fizic = 1,
    Online = 2,
    Hibrid = 3
}

public enum StatusSedinta
{
    Planificata = 1,
    Convocata = 2,
    InDesfasurare = 3,
    Finalizata = 4,
    Anulata = 5
}

public enum TipPunct
{
    ProiectHCL = 1,
    Informare = 2,
    Diverse = 3
}

public enum TipMajoritate
{
    Simpla = 1,
    Absoluta = 2,
    Calificata = 3
}

public enum RezultatPunct
{
    Adoptat = 1,
    Respins = 2,
    Amanat = 3,
    Retras = 4
}

public enum StatusPrezenta
{
    Prezent = 1,
    Absent = 2,
    AbsentMotivat = 3,
    OnlinePrezent = 4
}

public enum OptiuneVot
{
    Pentru = 1,
    Impotriva = 2,
    Abtinere = 3
}

public enum StatusProcesVerbal
{
    Draft = 1,
    Finalizat = 2
}

public enum StatusTrimitere
{
    Trimisa = 1,
    Esuata = 2,
    FaraDestinatie = 3,
    InAsteptare = 4
}

public enum TipVot
{
    Nominal = 1,
    Secret = 2
}

public enum TipDocument
{
    ProiectHCL = 1,
    ExpunereDeMotive = 2,
    Aviz = 3,
    Raport = 4,
    Anexa = 5,
    Altele = 6
}

public enum CanalNotificare
{
    Email = 1,
    Sms = 2
}

public enum StatusIncercare
{
    Trimisa = 1,
    Esuata = 2
}