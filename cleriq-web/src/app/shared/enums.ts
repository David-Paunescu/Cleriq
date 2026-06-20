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