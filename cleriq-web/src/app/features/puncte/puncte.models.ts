import { RezultatPunct, TipMajoritate, TipPunct, TipVot } from '../../shared/enums';

export interface PunctOrdineZi {
  id: number;
  sedintaId: number;
  ordine: number;
  titlu: string;
  descriere: string | null;
  tip: TipPunct;
  necesitaVot: boolean;
  tipVot: TipVot;
  tipMajoritate: TipMajoritate | null;
  rezultat: RezultatPunct | null;
  institutieId: number;
  creatLa: string;
  hclId: number | null;
}

export interface CrearePunct {
  ordine: number;
  titlu: string;
  descriere: string | null;
  tip: TipPunct;
  necesitaVot: boolean;
  tipMajoritate: TipMajoritate | null;
  tipVot: TipVot | null;
}

export interface ActualizarePunct {
  ordine: number;
  titlu: string;
  descriere: string | null;
  tip: TipPunct;
  necesitaVot: boolean;
  tipMajoritate: TipMajoritate | null;
  tipVot: TipVot | null;
}

export interface RezultatVot {
  punctId: number;
  rezultat: RezultatPunct;
  tipMajoritate: TipMajoritate | null;
  totalConsilieriInFunctie: number;
  voturiPentru: number;
  voturiImpotriva: number;
  voturiAbtinere: number;
  totalVoturiExprimate: number;
  pragNecesar: number;
}