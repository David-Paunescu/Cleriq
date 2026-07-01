import {
  StatusActRedactional, TipDispozitie, RolSemnatarDispozitie, MotivInvalidare,
  CanalTransmiterePrefect, RaspunsPrefect
} from '../../shared/enums';

// === Răspunsuri ===

// Element de listă — oglindă DispozitieDto (slim).
export interface Dispozitie {
  id: number;
  numar: number | null;
  anNumerotare: number | null;
  tipDispozitie: TipDispozitie;
  titlu: string;
  dataEmitere: string;
  dataIntrareInVigoare: string | null;
  status: StatusActRedactional;
  estePublicat: boolean;
  dataPublicareMol: string | null;
  dataInvalidare: string | null;
  motivInvalidare: MotivInvalidare | null;
  sedintaId: number | null;
  institutieId: number;
  creatLa: string;
}

// Detalii hub — oglindă DispozitieDetaliiDto.
export interface DispozitieDetalii {
  id: number;
  numar: number | null;
  anNumerotare: number | null;
  tipDispozitie: TipDispozitie;
  titlu: string;
  continut: string | null;
  dataEmitere: string;
  dataIntrareInVigoare: string | null;   // backend NU îl setează (mereu null) — nu construim UI pe el
  status: StatusActRedactional;
  estePublicat: boolean;
  dataPublicareMol: string | null;
  aIntratInCircuit: boolean;
  esteSemnat: boolean;                    // are PDF semnat atașat (≠ status === Semnat)
  numeFisierSemnat: string | null;
  marimeSemnat: number | null;
  dataIncarcareSemnat: string | null;
  contrasemnaturaRefuzata: boolean;
  obiectieLegalitateSecretar: string | null;
  dataRefuzContrasemnare: string | null;
  dataInvalidare: string | null;
  motivInvalidare: MotivInvalidare | null;
  motivInvalidareEticheta: string | null;  // label act-aware de la backend (afișat pe detaliu)
  refInvalidare: string | null;
  motivInvalidareAltulText: string | null;
  sedintaId: number | null;
  institutieId: number;
  creatLa: string;
  semnatari: SemnatarDispozitie[];
  comunicari: ComunicareDispozitiePrefect[];
}

export interface SemnatarDispozitie {
  id: number;
  rolSemnatar: RolSemnatarDispozitie;
  persoanaId: number | null;
  consilierId: number | null;
  nume: string;
  dataSemnare: string;
  ordineAfisare: number;
}

export interface ComunicareDispozitiePrefect {
  id: number;
  dispozitieId: number;
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

// === Cereri ===

// EmitentConsilierId = override viceprimar înlocuitor (art. 163); null → primarul derivat de backend.
export interface CreareDispozitie {
  tipDispozitie: TipDispozitie;
  titlu: string;
  dataEmitere: string;                // DateOnly „yyyy-MM-dd" din <input type="date">, fără fus
  emitentConsilierId: number | null;
}

export interface EditareContinutDispozitie {
  continut: string;
}

export interface AtribuireNumarDispozitie {
  numar: number;
  confirmaCuLacune: boolean;
}

export interface RefuzContrasemnare {
  obiectieLegalitate: string;
}

// Fără confirmaCuRelatiiActive (relațiile între dispoziții = Faza 7).
export interface InvalidareDispozitie {
  motiv: MotivInvalidare;
  motivAltulText: string | null;
  refInvalidare: string | null;
}

// Datele sunt DateOnly (API „yyyy-MM-dd") — vin din <input type="date">, fără conversie de fus.
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

export function formateazaMarime(octeti: number): string {
  if (octeti < 1024) return `${octeti} B`;
  if (octeti < 1024 * 1024) return `${(octeti / 1024).toFixed(1)} KB`;
  return `${(octeti / (1024 * 1024)).toFixed(1)} MB`;
}
