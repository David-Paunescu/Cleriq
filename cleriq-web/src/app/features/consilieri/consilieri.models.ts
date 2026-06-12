export interface Consilier {
  id: number;
  numeComplet: string;
  email: string | null;
  telefon: string | null;
  activ: boolean;
  institutieId: number;
  creatLa: string;
  areCont: boolean;
}

export interface CreareConsilier {
  numeComplet: string;
  email: string | null;
  telefon: string | null;
}

export interface ActualizareConsilier {
  numeComplet: string;
  email: string | null;
  telefon: string | null;
  activ: boolean;
}

export interface CreareContConsilier {
  email: string;
  parola: string;
}

export interface ContConsilier {
  utilizatorId: number;
  email: string;
  consilierId: number;
}