import { ModDesfasurare, StatusSedinta, TipSedinta, OptiuneVot, RezultatPunct, StatusPrezenta,
         TipMajoritate, TipPunct, TipVot, TipDocument, StatusTrimitere, StatusConvocare,
         CanalNotificare, StatusIncercare, StatusTranscriere, StatusProcesVerbal, TipFunctie,
         RolComisie, StatusActRedactional, TipHcl, RolSemnatar, TipRelatieHcl, MotivInvalidare,
         CanalTransmiterePrefect, RaspunsPrefect, TipDocumentHcl, TipDispozitie,
         RolSemnatarDispozitie  } from './enums';

export function etichetaTipSedinta(t: TipSedinta): string {
  switch (t) {
    case TipSedinta.Ordinara: return 'Ordinară';
    case TipSedinta.Extraordinara: return 'Extraordinară';
    case TipSedinta.DeIndata: return 'De îndată';
  }
}

export function etichetaModDesfasurare(m: ModDesfasurare): string {
  switch (m) {
    case ModDesfasurare.Fizic: return 'Fizic';
    case ModDesfasurare.Online: return 'Online';
    case ModDesfasurare.Hibrid: return 'Hibrid';
  }
}

export function etichetaStatusSedinta(s: StatusSedinta): string {
  switch (s) {
    case StatusSedinta.Planificata: return 'Planificată';
    case StatusSedinta.Convocata: return 'Convocată';
    case StatusSedinta.InDesfasurare: return 'În desfășurare';
    case StatusSedinta.Finalizata: return 'Finalizată';
    case StatusSedinta.Anulata: return 'Anulată';
  }
}

export function etichetaTipPunct(tip: TipPunct): string {
  switch (tip) {
    case TipPunct.ProiectHCL: return 'Proiect HCL';
    case TipPunct.Informare: return 'Informare';
    case TipPunct.Diverse: return 'Diverse';
  }
}

export function etichetaTipMajoritate(tip: TipMajoritate | null | undefined): string {
  if (tip == null) return '—';
  switch (tip) {
    case TipMajoritate.Simpla: return 'Simplă';
    case TipMajoritate.Absoluta: return 'Absolută';
    case TipMajoritate.Calificata: return 'Calificată';
  }
}

export function etichetaTipVot(tip: TipVot): string {
  switch (tip) {
    case TipVot.Nominal: return 'Nominal';
    case TipVot.Secret: return 'Secret';
  }
}

export function etichetaRezultatPunct(rezultat: RezultatPunct | null | undefined): string {
  if (rezultat == null) return 'Vot deschis';
  switch (rezultat) {
    case RezultatPunct.Adoptat: return 'Adoptat';
    case RezultatPunct.Respins: return 'Respins';
    case RezultatPunct.Amanat: return 'Amânat';
    case RezultatPunct.Retras: return 'Retras';
  }
}

export function etichetaOptiuneVot(o: OptiuneVot): string {
  switch (o) {
    case OptiuneVot.Pentru: return 'Pentru';
    case OptiuneVot.Impotriva: return 'Împotrivă';
    case OptiuneVot.Abtinere: return 'Abținere';
  }
}

export function etichetaStatusPrezenta(s: StatusPrezenta): string {
  switch (s) {
    case StatusPrezenta.Prezent: return 'Prezent';
    case StatusPrezenta.OnlinePrezent: return 'Prezent online';
    case StatusPrezenta.AbsentMotivat: return 'Absent motivat';
    case StatusPrezenta.Absent: return 'Absent';
  }
}

export function etichetaTipDocument(t: TipDocument): string {
  switch (t) {
    case TipDocument.ProiectHCL: return 'Proiect HCL';
    case TipDocument.ExpunereDeMotive: return 'Expunere de motive';
    case TipDocument.Aviz: return 'Aviz';
    case TipDocument.Raport: return 'Raport';
    case TipDocument.Anexa: return 'Anexă';
    case TipDocument.Altele: return 'Alt document';
  }
}
export function etichetaStatusTrimitere(s: StatusTrimitere | null | undefined): string {
  if (s == null) return '—';
  switch (s) {
    case StatusTrimitere.Trimisa: return 'Trimis';
    case StatusTrimitere.Esuata: return 'Eșuat';
    case StatusTrimitere.FaraDestinatie: return 'Fără destinație';
    case StatusTrimitere.InAsteptare: return 'În așteptare';
  }
}

export function etichetaStatusConvocare(s: StatusConvocare): string {
  switch (s) {
    case StatusConvocare.TotalSucces: return 'Trimis cu succes';
    case StatusConvocare.PartialSucces: return 'Parțial trimis';
    case StatusConvocare.Esuata: return 'Eșuat';
    case StatusConvocare.FaraCoordonate: return 'Fără coordonate';
    case StatusConvocare.InCursDeTrimitere: return 'În curs de trimitere';
  }
}

export function etichetaCanalNotificare(c: CanalNotificare): string {
  switch (c) {
    case CanalNotificare.Email: return 'Email';
    case CanalNotificare.Sms: return 'SMS';
  }
}

export function etichetaStatusIncercare(s: StatusIncercare): string {
  switch (s) {
    case StatusIncercare.Trimisa: return 'Trimisă';
    case StatusIncercare.Esuata: return 'Eșuată';
  }
}

export function etichetaStatusTranscriere(s: StatusTranscriere): string {
  switch (s) {
    case StatusTranscriere.InAsteptare: return 'În așteptare';
    case StatusTranscriere.InProces: return 'În procesare';
    case StatusTranscriere.Finalizata: return 'Finalizată';
    case StatusTranscriere.Esuata: return 'Eșuată';
  }
}

export function etichetaStatusProcesVerbal(s: StatusProcesVerbal): string {
  switch (s) {
    case StatusProcesVerbal.Draft: return 'Draft';
    case StatusProcesVerbal.Finalizat: return 'Finalizat';
  }
}

export function etichetaTipFunctie(t: TipFunctie): string {
  switch (t) {
    case TipFunctie.Primar: return 'Primar';
    case TipFunctie.Viceprimar: return 'Viceprimar';
    case TipFunctie.SecretarUat: return 'Secretar UAT';
  }
}

export function etichetaRolComisie(r: RolComisie): string {
  switch (r) {
    case RolComisie.Presedinte: return 'Președinte';
    case RolComisie.Secretar: return 'Secretar';
    case RolComisie.Membru: return 'Membru';
  }
}

// === HCL (Modul A) ===

export function etichetaStatusActRedactional(s: StatusActRedactional): string {
  switch (s) {
    case StatusActRedactional.Draft: return 'Draft';
    case StatusActRedactional.Numerotat: return 'Numerotat';
    case StatusActRedactional.Semnat: return 'Semnat';
  }
}

export function etichetaTipHcl(t: TipHcl): string {
  switch (t) {
    case TipHcl.Normativ: return 'Normativ';
    case TipHcl.Individual: return 'Individual';
  }
}

export function etichetaRolSemnatar(r: RolSemnatar): string {
  switch (r) {
    case RolSemnatar.PresedinteSedinta: return 'Președinte de ședință';
    case RolSemnatar.SecretarUat: return 'Secretar UAT';
    case RolSemnatar.SemnatarAlternativArt140: return 'Semnatar alternativ (art. 140)';
  }
}

export function etichetaTipRelatieHcl(t: TipRelatieHcl): string {
  switch (t) {
    case TipRelatieHcl.Modifica: return 'Modifică';
    case TipRelatieHcl.Abroga: return 'Abrogă';
    case TipRelatieHcl.Suspenda: return 'Suspendă';
    case TipRelatieHcl.PuneInAplicare: return 'Pune în aplicare';
    case TipRelatieHcl.Completeaza: return 'Completează';
    case TipRelatieHcl.Republica: return 'Republică';
  }
}

export function etichetaTipRelatieHclPasiv(t: TipRelatieHcl): string {
  switch (t) {
    case TipRelatieHcl.Modifica: return 'Modificată de';
    case TipRelatieHcl.Abroga: return 'Abrogată de';
    case TipRelatieHcl.Suspenda: return 'Suspendată de';
    case TipRelatieHcl.PuneInAplicare: return 'Pusă în aplicare de';
    case TipRelatieHcl.Completeaza: return 'Completată de';
    case TipRelatieHcl.Republica: return 'Republicată de';
  }
}

export function etichetaMotivInvalidare(m: MotivInvalidare | null | undefined): string {
  if (m == null) return '—';
  switch (m) {
    case MotivInvalidare.AnulatInstanta: return 'Anulat de instanță';
    case MotivInvalidare.AbrogatHclUlterior: return 'Abrogat prin HCL ulterior';
    case MotivInvalidare.Retractat: return 'Retractat';
    case MotivInvalidare.Caduc: return 'Caducitate (expirarea termenului / dispariția obiectului)';
    case MotivInvalidare.Inexistent: return 'Inexistent (nepublicat în MOL / lipsă element esențial)';
    case MotivInvalidare.Altul: return 'Altul';
    default: return '—';  // valoare stocată necunoscută (ex. fostul „prefect")
  }
}

// Etichetă act-aware pentru DISPOZIȚIE (oglindă MotivInvalidare.EtichetaDispozitie din backend):
// „Retractat" și „AbrogatHclUlterior" au formulări proprii — cuvântul „hotărâre"/„HCL" din globalul HCL
// e greșit pe o dispoziție; restul deleagă la eticheta globală. Folosit în invalidare-dialog (alegerea
// motivului); afișarea pe detaliu folosește `motivInvalidareEticheta` venit direct de la backend.
export function etichetaMotivInvalidareDispozitie(m: MotivInvalidare | null | undefined): string {
  switch (m) {
    case MotivInvalidare.Retractat: return 'Revocat de primar (emitent)';
    case MotivInvalidare.AbrogatHclUlterior: return 'Abrogată prin dispoziție ulterioară';
    default: return etichetaMotivInvalidare(m);
  }
}

export function etichetaCanalTransmiterePrefect(c: CanalTransmiterePrefect): string {
  switch (c) {
    case CanalTransmiterePrefect.Posta: return 'Poștă';
    case CanalTransmiterePrefect.EmailOficial: return 'Email oficial';
    case CanalTransmiterePrefect.Curier: return 'Curier';
    case CanalTransmiterePrefect.Prezentare: return 'Prezentare';
    case CanalTransmiterePrefect.ePoartal: return 'e-Portal';
    case CanalTransmiterePrefect.Altul: return 'Altul';
  }
}

export function etichetaRaspunsPrefect(r: RaspunsPrefect | null | undefined): string {
  if (r == null) return 'Fără răspuns';
  switch (r) {
    case RaspunsPrefect.Acceptat: return 'Acceptat';
    case RaspunsPrefect.RespinsLegalitate: return 'Respins pe legalitate';
    case RaspunsPrefect.CereClarificari: return 'Cere clarificări';
    case RaspunsPrefect.FaraRaspuns: return 'Fără răspuns';
  }
}

export function etichetaTipDocumentHcl(t: TipDocumentHcl | null | undefined): string {
  if (t == null) return '—';
  switch (t) {
    case TipDocumentHcl.Anexa: return 'Anexă';
    case TipDocumentHcl.RaportSpecialitate: return 'Raport de specialitate';
    case TipDocumentHcl.ExpunereDeMotive: return 'Expunere de motive';
    case TipDocumentHcl.AvizComisie: return 'Aviz comisie';
    case TipDocumentHcl.Justificativ: return 'Document justificativ';
    case TipDocumentHcl.Altul: return 'Altul';
  }
}

// === Dispoziții (Modul C) ===

export function etichetaTipDispozitie(t: TipDispozitie): string {
  switch (t) {
    case TipDispozitie.Normativ: return 'Normativ';
    case TipDispozitie.Individual: return 'Individual';
  }
}

export function etichetaRolSemnatarDispozitie(r: RolSemnatarDispozitie): string {
  switch (r) {
    case RolSemnatarDispozitie.Emitent: return 'Emitent (primar)';
    case RolSemnatarDispozitie.SecretarContrasemnatura: return 'Secretar general (contrasemnătură)';
  }
}