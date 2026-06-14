import { TipDocument } from '../../shared/enums';

export interface Document {
  id: number;
  denumire: string;
  descriere: string | null;
  tipDocument: TipDocument;
  numeFisierOriginal: string;
  tipMime: string;
  marime: number;
  hashSha256: string;
  estePublic: boolean;
  ordine: number;
  sedintaId: number | null;
  punctId: number | null;
  creatLa: string;
}

export interface ActualizareDocument {
  denumire: string;
  descriere: string | null;
  tipDocument: TipDocument;
  ordine: number;
}

export interface SetareVizibilitate {
  estePublic: boolean;
}

export const MARIME_MAXIMA = 25 * 1024 * 1024;

export const EXTENSII_PERMISE = [
  '.pdf', '.doc', '.docx', '.xls', '.xlsx', '.jpg', '.jpeg', '.png'
] as const;

export function valideazaFisier(numeFisier: string, marime: number): string | null {
  if (marime <= 0) return 'Fișierul este gol.';
  if (marime > MARIME_MAXIMA)
    return `Fișierul depășește limita de ${MARIME_MAXIMA / (1024 * 1024)} MB.`;

  const extensie = obtineExtensie(numeFisier);
  if (!extensie || !EXTENSII_PERMISE.includes(extensie as typeof EXTENSII_PERMISE[number]))
    return `Extensie nepermisă. Permise: ${EXTENSII_PERMISE.join(', ')}.`;

  return null;
}

export function obtineExtensie(numeFisier: string): string {
  const idx = numeFisier.lastIndexOf('.');
  return idx >= 0 ? numeFisier.substring(idx).toLowerCase() : '';
}

export function deduceTipDocument(numeFisier: string): TipDocument {
  const lower = numeFisier.toLowerCase();
  if (/hcl|proiect/.test(lower)) return TipDocument.ProiectHCL;
  if (/expunere/.test(lower)) return TipDocument.ExpunereDeMotive;
  if (/aviz/.test(lower)) return TipDocument.Aviz;
  if (/raport/.test(lower)) return TipDocument.Raport;
  if (/anex/.test(lower)) return TipDocument.Anexa;
  return TipDocument.Anexa;
}

export function denumireDinNumeFisier(numeFisier: string): string {
  const idx = numeFisier.lastIndexOf('.');
  return idx >= 0 ? numeFisier.substring(0, idx) : numeFisier;
}

export function formateazaMarime(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
}