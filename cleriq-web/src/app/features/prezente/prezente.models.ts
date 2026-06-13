import { StatusPrezenta } from '../../shared/enums';

export interface Prezenta {
  consilierId: number;
  numeCompletConsilier: string;
  status: StatusPrezenta;
  oraSosire: string | null;
}

export interface SetarePrezenta {
  consilierId: number;
  status: StatusPrezenta;
  oraSosire: string | null;
}