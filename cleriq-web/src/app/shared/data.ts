import { FUS_INSTITUTIE } from '../core/config';

export function inputLocalLaUtcIso(input: string): string {
  const [datePart, timePart] = input.split('T');
  const [an, luna, zi] = datePart.split('-').map(Number);
  const [ora, minut] = timePart.split(':').map(Number);

  const tentative = new Date(Date.UTC(an, luna - 1, zi, ora, minut));
  const offset = offsetFusOrar(tentative, FUS_INSTITUTIE);

  return new Date(tentative.getTime() - offset * 60_000).toISOString();
}

export function utcLaInputLocal(iso: string): string {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: FUS_INSTITUTIE,
    hourCycle: 'h23',
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit'
  }).formatToParts(new Date(iso));
  const m = Object.fromEntries(parts.map(p => [p.type, p.value]));
  return `${m['year']}-${m['month']}-${m['day']}T${m['hour']}:${m['minute']}`;
}

export function formateazaDataOra(iso: string): string {
  return new Intl.DateTimeFormat('ro-RO', {
    timeZone: FUS_INSTITUTIE,
    dateStyle: 'long',
    timeStyle: 'short'
  }).format(new Date(iso));
}

export function formateazaDataScurta(iso: string): string {
  return new Intl.DateTimeFormat('ro-RO', {
    timeZone: FUS_INSTITUTIE,
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  }).format(new Date(iso));
}

export function indicatorFus(iso: string): string {
  const offset = offsetFusOrar(new Date(iso), FUS_INSTITUTIE);
  const semn = offset >= 0 ? '+' : '-';
  const abs = Math.abs(offset);
  const ore = Math.floor(abs / 60);
  const min = abs % 60;
  return min === 0 ? `UTC${semn}${ore}` : `UTC${semn}${ore}:${String(min).padStart(2, '0')}`;
}

function offsetFusOrar(data: Date, fus: string): number {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: fus,
    hourCycle: 'h23',
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit'
  }).formatToParts(data);
  const m = Object.fromEntries(parts.map(p => [p.type, p.value]));
  const asUtc = Date.UTC(
    +m['year'], +m['month'] - 1, +m['day'],
    +m['hour'], +m['minute'], +m['second']);
  return Math.round((asUtc - data.getTime()) / 60_000);
}

// DateOnly (API: „yyyy-MM-dd") → afișare „dd.MM.yyyy", FĂRĂ shift de fus.
// Un DateOnly nu are oră/fus; helperele cu Europe/Bucharest ar adăuga o oră parazită
// (și ar muta ziua pe fusuri în urma UTC). Ancorăm la miezul nopții UTC și formatăm în
// UTC → corect-prin-construcție, indiferent de fusul instituției.
export function formateazaDataDoar(dataOnly: string | null | undefined): string {
  if (!dataOnly) return '—';
  return new Intl.DateTimeFormat('ro-RO', {
    timeZone: 'UTC',
    day: '2-digit', month: '2-digit', year: 'numeric'
  }).format(new Date(`${dataOnly}T00:00:00Z`));
}