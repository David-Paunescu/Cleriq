import { CanalNotificare, StatusConvocare, StatusIncercare, StatusTrimitere } from '../../shared/enums';

export interface Convocare {
  id: number;
  sedintaId: number;
  consilierId: number;
  numeCompletConsilier: string;
  emailStatus: StatusTrimitere | null;
  emailTrimisLa: string | null;
  emailDetalii: string | null;
  smsStatus: StatusTrimitere | null;
  smsTrimisLa: string | null;
  smsDetalii: string | null;
  statusGeneral: StatusConvocare;
  creatLa: string;
}

export interface IncercareTrimitere {
  id: number;
  canal: CanalNotificare;
  status: StatusIncercare;
  destinatar: string | null;
  detalii: string | null;
  creatLa: string;
}

export interface RezultatConvocare {
  totalConsilieri: number;
  totalSucces: number;
  inCursDeTrimitere: number;
  faraCoordonate: number;
  convocareTrimisaLa: string | null;
}

export function existaInAsteptare(convocari: Convocare[]): boolean {
  return convocari.some(c =>
    c.emailStatus === StatusTrimitere.InAsteptare
    || c.smsStatus === StatusTrimitere.InAsteptare);
}

export interface NumaratoriCanale {
  total: number;
  inAsteptare: number;
  trimise: number;
  esuate: number;
}

export function numaratori(convocari: Convocare[]): NumaratoriCanale {
  let inAsteptare = 0;
  let trimise = 0;
  let esuate = 0;

  for (const c of convocari) {
    for (const status of [c.emailStatus, c.smsStatus]) {
      if (status === StatusTrimitere.InAsteptare) inAsteptare++;
      else if (status === StatusTrimitere.Trimisa) trimise++;
      else if (status === StatusTrimitere.Esuata) esuate++;
    }
  }

  return { total: convocari.length, inAsteptare, trimise, esuate };
}

export function areCanalEsuat(convocare: Convocare): boolean {
  return convocare.emailStatus === StatusTrimitere.Esuata
      || convocare.smsStatus === StatusTrimitere.Esuata;
}