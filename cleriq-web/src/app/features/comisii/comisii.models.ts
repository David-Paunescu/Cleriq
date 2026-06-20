import { RolComisie } from '../../shared/enums';

export interface Comisie {
  id: number;
  denumire: string;
  descriere: string | null;
  institutieId: number;
  creatLa: string;
  membri: MembruComisie[];
}

export interface MembruComisie {
  consilierId: number;
  numeComplet: string;
  rol: RolComisie;
  dataInceput: string;
  dataSfarsit: string | null;
  dataInceputEstimata: boolean;
}

export interface CreareComisie {
  denumire: string;
  descriere: string | null;
}

export interface ActualizareComisie {
  denumire: string;
  descriere: string | null;
}

export interface AdaugareMembru {
  consilierId: number;
  rol: RolComisie;
  dataInceput: string;
}

export interface ActualizareDataInceputMembru {
  dataInceput: string;
}