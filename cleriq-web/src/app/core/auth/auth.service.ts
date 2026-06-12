import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CerereLogin, RaspunsLogin, UtilizatorCurent } from './auth.models';

const CHEIE_TOKEN = 'cleriq.token';
const CHEIE_REFRESH = 'cleriq.refreshToken';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly _utilizator = signal<UtilizatorCurent | null>(this.incarcaDinStocare());
  private refreshInFlight: Promise<boolean> | null = null;

  readonly utilizator = this._utilizator.asReadonly();
  readonly esteAutentificat = computed(() => this._utilizator() !== null);

  get token(): string | null {
    return localStorage.getItem(CHEIE_TOKEN);
  }

  async login(cerere: CerereLogin): Promise<void> {
    const raspuns = await firstValueFrom(
      this.http.post<RaspunsLogin>(`${environment.apiUrl}/api/Auth/login`, cerere));
    this.salveazaSesiune(raspuns);
  }

  logout(): void {
    const refreshToken = localStorage.getItem(CHEIE_REFRESH);
    if (refreshToken) {
      // Fire-and-forget: revocăm familia pe server fără să blocăm UI-ul.
      firstValueFrom(this.http.post(
        `${environment.apiUrl}/api/Auth/logout`, { refreshToken })).catch(() => undefined);
    }

    localStorage.removeItem(CHEIE_TOKEN);
    localStorage.removeItem(CHEIE_REFRESH);
    this._utilizator.set(null);
    this.router.navigate(['/login']);
  }

  // Single-flight: oricâte 401-uri simultane → un singur apel /refresh.
  reimprospateaza(): Promise<boolean> {
    this.refreshInFlight ??= this.executaRefresh()
      .finally(() => (this.refreshInFlight = null));
    return this.refreshInFlight;
  }

  areRol(rol: string): boolean {
    return this._utilizator()?.roluri.includes(rol) ?? false;
  }

  areOricareRol(...roluri: string[]): boolean {
    return roluri.some(r => this.areRol(r));
  }

  private async executaRefresh(): Promise<boolean> {
    const refreshToken = localStorage.getItem(CHEIE_REFRESH);
    if (!refreshToken) return false;

    try {
      const raspuns = await firstValueFrom(this.http.post<RaspunsLogin>(
        `${environment.apiUrl}/api/Auth/refresh`, { refreshToken }));
      this.salveazaSesiune(raspuns);
      return true;
    } catch {
      // Cursă multi-tab: alt tab a rotit deja tokenul → adoptăm sesiunea lui.
      const actual = localStorage.getItem(CHEIE_REFRESH);
      if (actual && actual !== refreshToken) {
        const utilizator = this.decodifica(localStorage.getItem(CHEIE_TOKEN) ?? '');
        if (utilizator) {
          this._utilizator.set(utilizator);
          return true;
        }
      }
      return false;
    }
  }

  private salveazaSesiune(raspuns: RaspunsLogin): void {
    localStorage.setItem(CHEIE_TOKEN, raspuns.token);
    localStorage.setItem(CHEIE_REFRESH, raspuns.refreshToken);
    this._utilizator.set(this.decodifica(raspuns.token));
  }

  private incarcaDinStocare(): UtilizatorCurent | null {
    const token = localStorage.getItem(CHEIE_TOKEN);
    if (!token) return null;

    const utilizator = this.decodifica(token);
    if (utilizator === null)
      localStorage.removeItem(CHEIE_TOKEN);
    // Refresh token-ul NU se șterge: access expirat + refresh valid = cazul
    // normal la revenire; authGuard declanșează reimprospateaza().

    return utilizator;
  }

  // Decodifică payload-ul JWT (base64url + UTF-8, pentru diacritice în NumeComplet).
  private decodifica(token: string): UtilizatorCurent | null {
    try {
      const b64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
      const bytes = Uint8Array.from(atob(b64), c => c.charCodeAt(0));
      const p = JSON.parse(new TextDecoder().decode(bytes));

      const exp = Number(p['exp']);
      if (!exp || exp * 1000 <= Date.now()) return null;

      const roluriBrut = p['role']
        ?? p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      const roluri: string[] = Array.isArray(roluriBrut)
        ? roluriBrut
        : roluriBrut ? [roluriBrut] : [];

      const idBrut = p['nameid']
        ?? p['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
      const emailBrut = p['email']
        ?? p['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'];

      return {
        id: Number(idBrut),
        email: emailBrut ?? '',
        numeComplet: p['NumeComplet'] ?? '',
        roluri,
        institutieId: Number(p['InstitutieId'] ?? 0),
        consilierId: p['ConsilierId'] != null ? Number(p['ConsilierId']) : null,
        expiraLa: new Date(exp * 1000)
      };
    } catch {
      return null;
    }
  }
}