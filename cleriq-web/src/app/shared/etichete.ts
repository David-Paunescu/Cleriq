import { ModDesfasurare, StatusSedinta, TipSedinta, OptiuneVot, RezultatPunct, StatusPrezenta, 
         TipMajoritate, TipPunct, TipVot, TipDocument, StatusTrimitere, StatusConvocare,
         CanalNotificare, StatusIncercare, StatusTranscriere } from './enums';

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