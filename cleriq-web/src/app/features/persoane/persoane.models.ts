export interface Persoana {
  id: number;
  numeComplet: string;
  email: string | null;
  telefon: string | null;
  institutieId: number;
  creatLa: string;
  areMandate: boolean;
}

export interface CrearePersoana {
  numeComplet: string;
  email: string | null;
  telefon: string | null;
}

export interface ActualizarePersoana {
  numeComplet: string;
  email: string | null;
  telefon: string | null;
}