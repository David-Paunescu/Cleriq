import { ModDesfasurare, StatusSedinta, TipSedinta } from '../../shared/enums';

export interface Sedinta {
  id: number;
  titlu: string;
  numar: string | null;
  tip: TipSedinta;
  dataOra: string;
  loc: string | null;
  modDesfasurare: ModDesfasurare;
  status: StatusSedinta;
  institutieId: number;
  creatLa: string;
  convocareTrimisaLa: string | null;
}

export interface CreareSedinta {
  titlu: string;
  numar: string | null;
  tip: TipSedinta;
  dataOra: string;
  loc: string | null;
  modDesfasurare: ModDesfasurare;
}

export interface ActualizareSedinta {
  titlu: string;
  numar: string | null;
  tip: TipSedinta;
  dataOra: string;
  loc: string | null;
  modDesfasurare: ModDesfasurare;
}