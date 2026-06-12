export interface CerereLogin {
  email: string;
  parola: string;
}

export interface RaspunsLogin {
  token: string;
  refreshToken: string;
}

export interface UtilizatorCurent {
  id: number;
  email: string;
  numeComplet: string;
  roluri: string[];
  institutieId: number;
  consilierId: number | null;
  expiraLa: Date;
}