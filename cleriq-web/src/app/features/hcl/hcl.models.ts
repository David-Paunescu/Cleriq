import {
  StatusHclRedactional, TipHcl, TipMajoritate, RolSemnatar, TipRelatieHcl,
  MotivInvalidare, CanalTransmiterePrefect, RaspunsPrefect, TipDocumentHcl
} from '../../shared/enums';

// === Răspunsuri ===

// Element de listă — oglindă HclDto.
export interface Hcl {
  id: number;
  numar: number | null;
  anNumerotare: number | null;
  tipHcl: TipHcl;
  titlu: string;
  dataAdoptare: string;
  dataIntrareInVigoare: string | null;
  status: StatusHclRedactional;
  estePublicat: boolean;
  dataPublicareMol: string | null;
  dataInvalidare: string | null;
  motivInvalidare: MotivInvalidare | null;
  institutieId: number;
  creatLa: string;
}

// Detalii hub — oglindă HclDetaliiDto.
export interface HclDetalii {
  id: number;
  numar: number | null;
  anNumerotare: number | null;
  tipHcl: TipHcl;
  titlu: string;
  continut: string | null;
  dataAdoptare: string;
  dataIntrareInVigoare: string | null;
  status: StatusHclRedactional;
  punctOrdineZiId: number;
  votPentru: number;
  votImpotriva: number;
  votAbtinere: number;
  tipMajoritate: TipMajoritate;
  estePublicat: boolean;
  dataPublicareMol: string | null;
  esteSemnat: boolean;
  numeFisierSemnat: string | null;
  marimeSemnat: number | null;
  dataIncarcareSemnat: string | null;
  motivLipsaSemnaturaPresedinte: string | null;
  dataInvalidare: string | null;
  motivInvalidare: MotivInvalidare | null;
  refInvalidare: string | null;
  institutieId: number;
  creatLa: string;
  semnatari: SemnatarHcl[];
  documente: DocumentHcl[];
  relatiiSursa: RelatieHcl[];
  relatiiTinta: RelatieHcl[];
  comunicari: ComunicareHclPrefect[];
}

export interface SemnatarHcl {
  id: number;
  rolSemnatar: RolSemnatar;
  persoanaId: number | null;
  consilierId: number | null;
  numeComplet: string;
  dataSemnare: string;
  ordineAfisare: number;
}

export interface DocumentHcl {
  id: number;
  denumire: string;
  descriere: string | null;
  tipDocumentHcl: TipDocumentHcl | null;
  numarOrdinAnexa: number | null;
  numeFisierOriginal: string;
  marime: number;
  ordine: number;
}

export interface RelatieHcl {
  id: number;
  tipRelatie: TipRelatieHcl;
  hclSursaId: number;
  numarSursaFormatat: string | null;
  titluSursa: string;
  hclTintaId: number | null;
  numarTintaFormatat: string | null;
  titluTinta: string | null;
  referintaActExternText: string | null;
}

export interface ComunicareHclPrefect {
  id: number;
  hclId: number;
  numarOrdineInRegistru: number;
  anRegistru: number;
  dataTrimiteri: string;
  dataInregistrareInRegistru: string;
  canalTransmitere: CanalTransmiterePrefect;
  nrInregistrarePrefect: string | null;
  dataConfirmarePrefect: string | null;
  obiectiiMotivate: string | null;
  raspunsPrefect: RaspunsPrefect | null;
  dataRaspunsPrefect: string | null;
  observatiiInterne: string | null;
  creatLa: string;
}

export interface SugestieNumar {
  numar: number;
  an: number;
}

// === Cereri (FE1) ===

export interface CreareHcl {
  punctOrdineZiId: number;
  tipHcl: TipHcl;
}

export interface EditareContinutHcl {
  continut: string;
}

export interface AtribuireNumarHcl {
  numar: number;
  confirmaCuLacune: boolean;
}
