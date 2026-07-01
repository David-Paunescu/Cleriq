import {
  StatusActRedactional, TipHcl, TipMajoritate, RolSemnatar, TipRelatieHcl,
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
  status: StatusActRedactional;
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
  status: StatusActRedactional;
  punctOrdineZiId: number;
  votPentru: number;
  votImpotriva: number;
  votAbtinere: number;
  tipMajoritate: TipMajoritate;
  estePublicat: boolean;
  dataPublicareMol: string | null;
  aIntratInCircuit: boolean;
  esteSemnat: boolean;
  numeFisierSemnat: string | null;
  marimeSemnat: number | null;
  dataIncarcareSemnat: string | null;
  motivLipsaSemnaturaPresedinte: string | null;
  dataInvalidare: string | null;
  motivInvalidare: MotivInvalidare | null;
  refInvalidare: string | null;
  motivInvalidareAltulText: string | null;
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

// Răspunsul GET /Hcl/{id}/Relatii — ambele direcții.
export interface RelatiiHcl {
  relatiiSursa: RelatieHcl[];
  relatiiTinta: RelatieHcl[];
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

// Vedere cronologică cross-HCL — oglindă RegistruComunicareDto.
export interface RegistruComunicare {
  id: number;
  numarOrdineInRegistru: number;
  anRegistru: number;
  dataTrimiteri: string;
  hclId: number;
  numarHclFormatat: string | null;
  titluHcl: string;
  canalTransmitere: CanalTransmiterePrefect;
  raspunsPrefect: RaspunsPrefect | null;
}

export interface SugestieNumar {
  numar: number;
  an: number;
}

// Widget T-3 „HCL urgent de comunicat" — oglindă HclUrgentDto. zileRamase poate fi negativ (depășit).
export interface HclUrgent {
  hclId: number;
  numar: number;
  anNumerotare: number;
  titlu: string;
  dataAdoptare: string;
  dataLimitaComunicare: string;
  zileRamase: number;
  status: StatusActRedactional;
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

// === Cereri (FE2) ===

export interface AdaugareSemnatar {
  rol: RolSemnatar;
  consilierId: number | null;
  persoanaId: number | null;
  ordineAfisare: number;
}

export interface InvalidareHcl {
  motiv: MotivInvalidare;
  motivAltulText: string | null;
  refInvalidare: string | null;
  confirmaCuRelatiiActive: boolean;
}

export interface PublicareHcl {
  estePublicat: boolean;
}

export interface PublicareMol {
  dataPublicareMol: string;  // DateOnly „yyyy-MM-dd" (din <input type="date">)
}

export interface MotivLipsaPresedinte {
  motiv: string;
}

// === Cereri (FE3) ===

// Datele sunt DateOnly (API: „yyyy-MM-dd") — vin direct din <input type="date">, fără conversie de fus.
export interface CreareComunicare {
  dataTrimiteri: string;
  canalTransmitere: CanalTransmiterePrefect;
  nrInregistrarePrefect: string | null;
  dataConfirmarePrefect: string | null;
  observatiiInterne: string | null;
}

export interface ActualizareComunicare {
  raspuns: RaspunsPrefect | null;
  dataRaspuns: string | null;
  obiectiiMotivate: string | null;
  observatiiInterne: string | null;
  nrInregistrarePrefect: string | null;
  dataConfirmarePrefect: string | null;
}

// XOR: ori hclTintaId (intern), ori referintaActExternText (extern) — validat și pe backend.
export interface CreareRelatie {
  hclTintaId: number | null;
  referintaActExternText: string | null;
  tipRelatie: TipRelatieHcl;
}

// Payload-ul 409 „relații active" întors de Invalidare fără confirmare (oglindă obiect anonim backend).
export interface RelatiiActiveInvalidare {
  mesaj: string;
  relatiiSursaActive: RelatieHcl[];
  relatiiTintaActive: RelatieHcl[];
}

export function formateazaMarime(octeti: number): string {
  if (octeti < 1024) return `${octeti} B`;
  if (octeti < 1024 * 1024) return `${(octeti / 1024).toFixed(1)} KB`;
  return `${(octeti / (1024 * 1024)).toFixed(1)} MB`;
}
