import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CerereLogin, RaspunsLogin, UtilizatorCurent } from './auth.models';

const CHEIE_TOKEN = 'cleriq.token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly _utilizator = signal<UtilizatorCurent | null>(this.incarcaDinStocare());

  readonly utilizator = this._utilizator.asReadonly();
  readonly esteAutentificat = computed(() => this._utilizator() !== null);

  get token(): string | null {
    return localStorage.getItem(CHEIE_TOKEN);
  }

  async login(cerere: CerereLogin): Promise<void> {
    const raspuns = await firstValueFrom(
      this.http.post<RaspunsLogin>(`${environment.apiUrl}/api/Auth/login`, cerere));

    localStorage.setItem(CHEIE_TOKEN, raspuns.token);
    this._utilizator.set(this.decodifica(raspuns.token));
  }

  logout(): void {
    localStorage.removeItem(CHEIE_TOKEN);
    this._utilizator.set(null);
    this.router.navigate(['/login']);
  }

  areRol(rol: string): boolean {
    return this._utilizator()?.roluri.includes(rol) ?? false;
  }

  areOricareRol(...roluri: string[]): boolean {
    return roluri.some(r => this.areRol(r));
  }

  private incarcaDinStocare(): UtilizatorCurent | null {
    const token = localStorage.getItem(CHEIE_TOKEN);
    if (!token) return null;

    const utilizator = this.decodifica(token);
    if (utilizator === null)
      localStorage.removeItem(CHEIE_TOKEN);

    return utilizator;
  }

  // Decodifică payload-ul JWT (base64url + UTF-8, pentru diacritice în NumeComplet).
  // JwtSecurityTokenHandler din .NET scurtează claim-urile standard (nameid, email, role);
  // păstrăm fallback pe URI-urile lungi, defensiv.
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