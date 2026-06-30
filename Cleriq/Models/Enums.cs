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

public enum SmtpSecuritate
{
    Auto = 1,
    StartTls = 2,
    SslDirect = 3
}

public enum StatusTranscriere
{
    InAsteptare = 1,
    InProces = 2,
    Finalizata = 3,
    Esuata = 4
}

public enum TipFunctie
{
    Primar = 1,
    Viceprimar = 2,
    SecretarUat = 3
}

public enum StatusHclRedactional
{
    Draft = 1,
    Numerotat = 2,
    Semnat = 3
}

public enum TipHcl
{
    Normativ = 1,
    Individual = 2
}

public enum TipRelatieHcl
{
    Modifica = 1,
    Abroga = 2,
    Suspenda = 3,
    PuneInAplicare = 4,
    Completeaza = 5,
    Republica = 6
}

public enum CanalTransmiterePrefect
{
    Posta = 1,
    EmailOficial = 2,
    Curier = 3,
    Prezentare = 4,
    ePoartal = 5,
    Altul = 6
}

public enum RolSemnatar
{
    PresedinteSedinta = 1,
    SecretarUat = 2,
    SemnatarAlternativArt140 = 3
}

public enum MotivInvalidare
{
    // 1 = fostul „AnulatPrefect", eliminat: prefectul atacă actul, instanța îl anulează
    // (Constituție art. 123 alin. 5, Cod adm. art. 255). NU se reintroduce valoarea 1.
    AnulatInstanta = 2,
    AbrogatHclUlterior = 3,
    Retractat = 4,
    Caduc = 5,
    Inexistent = 6,
    Altul = 7
}

public enum TipDocumentHcl
{
    Anexa = 1,
    RaportSpecialitate = 2,
    ExpunereDeMotive = 3,
    AvizComisie = 4,
    Justificativ = 5,
    Altul = 6
}

public enum RaspunsPrefect
{
    Acceptat = 1,
    RespinsLegalitate = 2,
    CereClarificari = 3,
    FaraRaspuns = 4
}

// Tipuri de acțiuni sensibile consemnate în jurnalul de audit al HCL (extensibil).
public enum TipActiuneHcl
{
    AnulareMol = 1
}