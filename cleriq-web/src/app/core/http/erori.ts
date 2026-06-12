import { HttpErrorResponse } from '@angular/common/http';

export function extrageMesajEroare(
  err: unknown,
  fallback = 'A apărut o eroare neașteptată. Încearcă din nou.'
): string {
  if (!(err instanceof HttpErrorResponse)) return fallback;
  if (err.status === 0) return 'Serverul nu poate fi contactat. Verifică conexiunea.';

  const corp: unknown = err.error;

  if (typeof corp === 'string' && corp.trim().length > 0) return corp;

  if (Array.isArray(corp)) {
    const mesaje = corp
      .map(extrageDescriere)
      .filter((m): m is string => m !== null);
    if (mesaje.length > 0) return mesaje.join(' ');
  }

  if (esteObiect(corp)) {
    if (esteObiect(corp['errors'])) {
      const mesaje = Object.values(corp['errors'])
        .flatMap(v => (Array.isArray(v) ? v.map(String) : []));
      if (mesaje.length > 0) return mesaje.join(' ');
    }
    if (typeof corp['mesaj'] === 'string') return corp['mesaj'];
    if (typeof corp['title'] === 'string') return corp['title'];
  }

  return fallback;
}

function extrageDescriere(e: unknown): string | null {
  return esteObiect(e) && typeof e['description'] === 'string' ? e['description'] : null;
}

function esteObiect(v: unknown): v is Record<string, unknown> {
  return typeof v === 'object' && v !== null;
}