import { OptiuneVot, TipVot } from '../../shared/enums';

export interface Vot {
  id: number;
  punctId: number;
  consilierId: number;
  numeCompletConsilier: string;
  optiune: OptiuneVot;
  dataOra: string;
  institutieId: number;
}

export interface VoturiPunct {
  punctId: number;
  tipVot: TipVot;
  pentru: number;
  impotriva: number;
  abtineri: number;
  totalExprimate: number;
  voturiNominale: Vot[];
  participanti: string[];
  votulMeu: OptiuneVot | null;
}

export interface InregistrareVot {
  consilierId: number;
  optiune: OptiuneVot;
}

export interface InregistrareVotSelf {
  optiune: OptiuneVot;
}