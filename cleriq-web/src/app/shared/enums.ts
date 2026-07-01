export enum TipSedinta {
  Ordinara = 1,
  Extraordinara = 2,
  DeIndata = 3
}

export enum ModDesfasurare {
  Fizic = 1,
  Online = 2,
  Hibrid = 3
}

export enum StatusSedinta {
  Planificata = 1,
  Convocata = 2,
  InDesfasurare = 3,
  Finalizata = 4,
  Anulata = 5
}

export enum TipPunct {
  ProiectHCL = 1,
  Informare = 2,
  Diverse = 3
}

export enum TipMajoritate {
  Simpla = 1,
  Absoluta = 2,
  Calificata = 3
}

export enum TipVot {
  Nominal = 1,
  Secret = 2
}

export enum RezultatPunct {
  Adoptat = 1,
  Respins = 2,
  Amanat = 3,
  Retras = 4
}

export enum OptiuneVot {
  Pentru = 1,
  Impotriva = 2,
  Abtinere = 3
}

export enum StatusPrezenta {
  Prezent = 1,
  Absent = 2,
  AbsentMotivat = 3,
  OnlinePrezent = 4
}

export enum TipDocument {
  ProiectHCL = 1,
  ExpunereDeMotive = 2,
  Aviz = 3,
  Raport = 4,
  Anexa = 5,
  Altele = 6
}

export enum StatusTrimitere {
  Trimisa = 1,
  Esuata = 2,
  FaraDestinatie = 3,
  InAsteptare = 4
}

export enum StatusConvocare {
  TotalSucces = 1,
  PartialSucces = 2,
  Esuata = 3,
  FaraCoordonate = 4,
  InCursDeTrimitere = 5
}

export enum CanalNotificare {
  Email = 1,
  Sms = 2
}

export enum StatusIncercare {
  Trimisa = 1,
  Esuata = 2
}

export enum StatusTranscriere {
  InAsteptare = 1,
  InProces = 2,
  Finalizata = 3,
  Esuata = 4
}

export enum StatusProcesVerbal {
  Draft = 1,
  Finalizat = 2
}

export enum TipFunctie {
  Primar = 1,
  Viceprimar = 2,
  SecretarUat = 3
}

export enum RolComisie {
  Presedinte = 1,
  Secretar = 2,
  Membru = 3
}

// === HCL (Modul A) ===

export enum StatusActRedactional {
  Draft = 1,
  Numerotat = 2,
  Semnat = 3
}

export enum TipHcl {
  Normativ = 1,
  Individual = 2
}

export enum RolSemnatar {
  PresedinteSedinta = 1,
  SecretarUat = 2,
  SemnatarAlternativArt140 = 3
}

export enum TipRelatieHcl {
  Modifica = 1,
  Abroga = 2,
  Suspenda = 3,
  PuneInAplicare = 4,
  Completeaza = 5,
  Republica = 6
}

export enum MotivInvalidare {
  // 1 = fostul „AnulatPrefect", eliminat: prefectul atacă, instanța anulează. NU se reintroduce.
  AnulatInstanta = 2,
  AbrogatHclUlterior = 3,
  Retractat = 4,
  Caduc = 5,
  Inexistent = 6,
  Altul = 7
}

export enum CanalTransmiterePrefect {
  Posta = 1,
  EmailOficial = 2,
  Curier = 3,
  Prezentare = 4,
  ePoartal = 5,
  Altul = 6
}

export enum RaspunsPrefect {
  Acceptat = 1,
  RespinsLegalitate = 2,
  CereClarificari = 3,
  FaraRaspuns = 4
}

export enum TipDocumentHcl {
  Anexa = 1,
  RaportSpecialitate = 2,
  ExpunereDeMotive = 3,
  AvizComisie = 4,
  Justificativ = 5,
  Altul = 6
}

// === Dispoziții (Modul C) ===

export enum TipDispozitie {
  Normativ = 1,
  Individual = 2
}

export enum RolSemnatarDispozitie {
  Emitent = 1,
  SecretarContrasemnatura = 2
}