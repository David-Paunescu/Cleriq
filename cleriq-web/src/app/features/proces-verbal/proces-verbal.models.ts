import { StatusProcesVerbal } from '../../shared/enums';

export interface ProcesVerbal {
  id: number;
  sedintaId: number;
  continut: string | null;
  status: StatusProcesVerbal;
  dataGenerare: string | null;
  dataFinalizare: string | null;
  institutieId: number;
  creatLa: string;
  numeFisierSemnat: string | null;
  marimeSemnat: number | null;
  dataIncarcareSemnat: string | null;
  dataAprobare: string | null;
  aprobatInSedintaId: number | null;
  aprobatInSedintaTitlu: string | null;
  aprobatInSedintaDataOra: string | null;
}

export interface EditareProcesVerbal {
  continut: string;
}

export interface AprobareProcesVerbal {
  aprobatInSedintaId: number;
}

export const MARIME_MAXIMA_PV = 25 * 1024 * 1024;
export const EXTENSIE_PERMISA_PV = '.pdf';

export function valideazaPdfSemnat(numeFisier: string, marime: number): string | null {
  if (marime <= 0) return 'Fișierul este gol.';
  if (marime > MARIME_MAXIMA_PV)
    return `Fișierul depășește limita de ${MARIME_MAXIMA_PV / (1024 * 1024)} MB.`;

  const extensie = obtineExtensie(numeFisier);
  if (extensie !== EXTENSIE_PERMISA_PV)
    return 'Doar fișiere PDF sunt acceptate pentru procesul verbal semnat.';

  return null;
}

export function obtineExtensie(numeFisier: string): string {
  const idx = numeFisier.lastIndexOf('.');
  return idx >= 0 ? numeFisier.substring(idx).toLowerCase() : '';
}

export function formateazaMarimePv(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
}