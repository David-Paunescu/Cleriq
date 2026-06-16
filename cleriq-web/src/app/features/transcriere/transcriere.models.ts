import { StatusTranscriere } from '../../shared/enums';

export interface Transcriere {
  id: number;
  sedintaId: number;
  status: StatusTranscriere;
  dataPrimireBrut: string | null;
  dataUltimeiEditari: string | null;
  dimensiuneAudio: number;
  durataAudioSecunde: number | null;
  modelFolosit: string;
  numarEsecuri: number;
  urmatoareaIncercareDupa: string | null;
  ultimaEroare: string | null;
  institutieId: number;
  creatLa: string;
  dataPublicare: string | null;
  publicataDe: number | null;
}

export interface TranscriereContinut {
  id: number;
  sedintaId: number;
  status: StatusTranscriere;
  continutBrut: string | null;
  continutEditat: string | null;
  continutPublicat: string | null;
}

export interface EditareTranscriere {
  continutEditat: string;
}

export const MARIME_MAXIMA = 500 * 1024 * 1024;

export const EXTENSII_PERMISE = [
  '.mp3', '.wav', '.m4a', '.ogg', '.flac', '.aac'
] as const;

export function valideazaAudio(numeFisier: string, marime: number): string | null {
  if (marime <= 0) return 'Fișierul audio este gol.';
  if (marime > MARIME_MAXIMA)
    return `Fișierul audio depășește limita de ${MARIME_MAXIMA / (1024 * 1024)} MB.`;

  const extensie = obtineExtensie(numeFisier);
  if (!extensie || !EXTENSII_PERMISE.includes(extensie as typeof EXTENSII_PERMISE[number]))
    return `Extensie audio nepermisă. Permise: ${EXTENSII_PERMISE.join(', ')}.`;

  return null;
}

export function obtineExtensie(numeFisier: string): string {
  const idx = numeFisier.lastIndexOf('.');
  return idx >= 0 ? numeFisier.substring(idx).toLowerCase() : '';
}

export function formateazaMarimeAudio(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`;
}

export function formateazaDurataAudio(secunde: number | null): string {
  if (secunde === null || secunde <= 0) return '—';
  const h = Math.floor(secunde / 3600);
  const m = Math.floor((secunde % 3600) / 60);
  const s = Math.floor(secunde % 60);
  if (h > 0) return `${h}h ${m}m ${s}s`;
  if (m > 0) return `${m}m ${s}s`;
  return `${s}s`;
}

export interface Segment {
  start: number;
  end: number;
  text: string;
  speaker: string;
}

export interface OptiuniFormatare {
  speaker: boolean;
  timestamp: boolean;
}

export function parseazaContinutBrut(json: string | null): Segment[] {
  if (!json) return [];

  try {
    const parsed = JSON.parse(json) as Record<string, unknown>;
    const segmente = (parsed['text'] ?? parsed['segments']) as unknown;

    if (!Array.isArray(segmente)) return [];

    return segmente
      .map(s => parseazaSegment(s))
      .filter((s): s is Segment => s !== null);
  } catch {
    return [];
  }
}

function parseazaSegment(s: unknown): Segment | null {
  if (typeof s !== 'object' || s === null) return null;
  const seg = s as Record<string, unknown>;

  if (typeof seg['text'] !== 'string') return null;

  return {
    start: typeof seg['start'] === 'number' ? seg['start'] : 0,
    end: typeof seg['end'] === 'number' ? seg['end'] : 0,
    text: (seg['text'] as string).trim(),
    speaker: typeof seg['speaker'] === 'string' ? seg['speaker'] : 'SPEAKER_UNKNOWN'
  };
}

export function formateazaTimp(secunde: number): string {
  const h = Math.floor(secunde / 3600);
  const m = Math.floor((secunde % 3600) / 60);
  const s = Math.floor(secunde % 60);
  const pad = (n: number): string => n.toString().padStart(2, '0');
  return `${pad(h)}:${pad(m)}:${pad(s)}`;
}

export function formateazaSegmente(
  segmente: Segment[],
  optiuni: OptiuniFormatare
): string {
  return segmente.map(s => {
    let prefix = '';
    if (optiuni.timestamp) prefix += `[${formateazaTimp(s.start)}] `;
    if (optiuni.speaker) prefix += `[${s.speaker}]: `;
    return prefix + s.text;
  }).join('\n');
}