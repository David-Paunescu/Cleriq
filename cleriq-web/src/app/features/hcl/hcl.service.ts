import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { StatusHclRedactional, TipHcl } from '../../shared/enums';
import {
  AtribuireNumarHcl, CreareHcl, EditareContinutHcl, Hcl, HclDetalii, SugestieNumar
} from './hcl.models';

export interface FiltreHcl {
  an?: number;
  status?: StatusHclRedactional;
  tipHcl?: TipHcl;
}

@Injectable({ providedIn: 'root' })
export class HclService {
  private readonly http = inject(HttpClient);
  private readonly urlBaza = `${environment.apiUrl}/api/Hcl`;

  lista(filtre: FiltreHcl = {}): Promise<Hcl[]> {
    let params = new HttpParams();
    if (filtre.an != null) params = params.set('an', filtre.an);
    if (filtre.status != null) params = params.set('status', filtre.status);
    if (filtre.tipHcl != null) params = params.set('tipHcl', filtre.tipHcl);
    return firstValueFrom(this.http.get<Hcl[]>(this.urlBaza, { params }));
  }

  detalii(id: number): Promise<HclDetalii> {
    return firstValueFrom(this.http.get<HclDetalii>(`${this.urlBaza}/${id}`));
  }

  // Întoarce HclDto (slim) — apelantul navighează la /hcl/:id, care reîncarcă Detalii.
  genereaza(cerere: CreareHcl): Promise<Hcl> {
    return firstValueFrom(this.http.post<Hcl>(`${this.urlBaza}/Genereaza`, cerere));
  }

  // Mutațiile întorc HclDetalii complet (vezi pasul 0 backend) → set direct în hub.
  editeazaContinut(id: number, cerere: EditareContinutHcl): Promise<HclDetalii> {
    return firstValueFrom(this.http.put<HclDetalii>(`${this.urlBaza}/${id}/Continut`, cerere));
  }

  regenereazaContinut(id: number): Promise<HclDetalii> {
    return firstValueFrom(
      this.http.post<HclDetalii>(`${this.urlBaza}/${id}/RegenereazaContinut`, {}));
  }

  sugestieNumar(id: number): Promise<SugestieNumar> {
    return firstValueFrom(this.http.get<SugestieNumar>(`${this.urlBaza}/${id}/SugestieNumar`));
  }

  atribuieNumar(id: number, cerere: AtribuireNumarHcl): Promise<HclDetalii> {
    return firstValueFrom(
      this.http.post<HclDetalii>(`${this.urlBaza}/${id}/AtribuieNumar`, cerere));
  }

  semneaza(id: number): Promise<HclDetalii> {
    return firstValueFrom(this.http.post<HclDetalii>(`${this.urlBaza}/${id}/Semneaza`, {}));
  }

  async descarcaPdf(id: number, numeAfisat: string): Promise<void> {
    const blob = await firstValueFrom(
      this.http.get(`${this.urlBaza}/${id}/Pdf`, { responseType: 'blob' }));
    this.declanseazaDescarcare(blob, numeAfisat);
  }

  private declanseazaDescarcare(blob: Blob, numeFisier: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = numeFisier;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
}
