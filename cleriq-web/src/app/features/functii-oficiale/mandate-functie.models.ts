import { TipFunctie } from '../../shared/enums';

export interface MandatFunctie {
  id: number;
  tipFunctie: TipFunctie;
  persoanaId: number | null;
  numeCompletPersoana: string | null;
  consilierId: number | null;
  numeCompletConsilier: string | null;
  dataInceput: string;
  dataSfarsit: string | null;
  nrActNumire: string | null;
  institutieId: number;
  creatLa: string;
}

export interface CreareMandatFunctie {
  tipFunctie: TipFunctie;
  persoanaId: number | null;
  consilierId: number | null;
  dataInceput: string;
  dataSfarsit: string | null;
  nrActNumire: string | null;
}

export interface ActualizareMandatFunctie {
  dataInceput: string;
  dataSfarsit: string | null;
  nrActNumire: string | null;
}

export interface InchideMandatFunctie {
  dataSfarsit: string;
}