import { RolComisie } from '../../shared/enums';

export interface SubiectIstoric {
  id: number;
  numeComplet: string;
}

export interface ViceprimarIstoric {
  consilierId: number;
  numeComplet: string;
  mandatFunctieId: number;
  dataInceput: string;
  dataSfarsit: string | null;
}

export interface MembruIstoric {
  consilierId: number;
  numeComplet: string;
  rol: RolComisie;
}