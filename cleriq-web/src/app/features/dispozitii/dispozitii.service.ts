import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { StatusActRedactional, TipDispozitie } from '../../shared/enums';
import {
  ActualizareComunicare, AtribuireNumarDispozitie, ComunicareDispozitiePrefect, CreareComunicare,
  CreareDispozitie, Dispozitie, DispozitieDetalii, EditareContinutDispozitie, InvalidareDispozitie,
  SugestieNumar
} from './dispozitii.models';

export interface FiltreDispozitii {
  an?: number;
  status?: StatusActRedactional;
  tip?: TipDispozitie;
  skip?: number;
  take?: number;
}

@Injectable({ providedIn: 'root' })
export class DispozitiiService {
  private readonly http = inject(HttpClient);
  private readonly urlBaza = `${environment.apiUrl}/api/Dispozitii`;

  lista(filtre: FiltreDispozitii = {}): Promise<Dispozitie[]> {
    let params = new HttpParams();
    if (filtre.an != null) params = params.set('an', filtre.an);
    if (filtre.status != null) params = params.set('status', filtre.status);
    if (filtre.tip != null) params = params.set('tip', filtre.tip);
    if (filtre.skip != null) params = params.set('skip', filtre.skip);
    if (filtre.take != null) params = params.set('take', filtre.take);
    return firstValueFrom(this.http.get<Dispozitie[]>(this.urlBaza, { params }));
  }

  detalii(id: number): Promise<DispozitieDetalii> {
    return firstValueFrom(this.http.get<DispozitieDetalii>(`${this.urlBaza}/${id}`));
  }

  // ◆ Creeaza întoarce DispozitieDetalii complet (nu slim) → apelantul navighează la /dispozitii/:id.
  creeaza(cerere: CreareDispozitie): Promise<DispozitieDetalii> {
    return firstValueFrom(this.http.post<DispozitieDetalii>(this.urlBaza, cerere));
  }

  // Mutațiile întorc DispozitieDetalii complet → set direct în hub (un singur round-trip).
  editeazaContinut(id: number, cerere: EditareContinutDispozitie): Promise<DispozitieDetalii> {
    return firstValueFrom(this.http.put<DispozitieDetalii>(`${this.urlBaza}/${id}/Continut`, cerere));
  }

  regenereazaContinut(id: number): Promise<DispozitieDetalii> {
    return firstValueFrom(
      this.http.post<DispozitieDetalii>(`${this.urlBaza}/${id}/RegenereazaContinut`, {}));
  }

  sugestieNumar(id: number): Promise<SugestieNumar> {
    return firstValueFrom(this.http.get<SugestieNumar>(`${this.urlBaza}/${id}/SugestieNumar`));
  }

  atribuieNumar(id: number, cerere: AtribuireNumarDispozitie): Promise<DispozitieDetalii> {
    return firstValueFrom(
      this.http.post<DispozitieDetalii>(`${this.urlBaza}/${id}/AtribuieNumar`, cerere));
  }

  semneaza(id: number): Promise<DispozitieDetalii> {
    return firstValueFrom(this.http.post<DispozitieDetalii>(`${this.urlBaza}/${id}/Semneaza`, {}));
  }

  refuzaContrasemnare(id: number, obiectieLegalitate: string): Promise<DispozitieDetalii> {
    return firstValueFrom(this.http.post<DispozitieDetalii>(
      `${this.urlBaza}/${id}/RefuzContrasemnare`, { obiectieLegalitate }));
  }

  // === Variantă semnată (PDF scanat) ===
  incarcaSemnat(id: number, fisier: File): Promise<DispozitieDetalii> {
    const form = new FormData();
    form.append('fisier', fisier);
    return firstValueFrom(this.http.post<DispozitieDetalii>(`${this.urlBaza}/${id}/Semnat`, form));
  }

  async descarcaSemnat(id: number, numeAfisat: string): Promise<void> {
    const blob = await firstValueFrom(
      this.http.get(`${this.urlBaza}/${id}/Semnat`, { responseType: 'blob' }));
    this.declanseazaDescarcare(blob, numeAfisat);
  }

  stergeSemnat(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.urlBaza}/${id}/Semnat`));
  }

  async descarcaPdf(id: number, numeAfisat: string): Promise<void> {
    const blob = await firstValueFrom(
      this.http.get(`${this.urlBaza}/${id}/Pdf`, { responseType: 'blob' }));
    this.declanseazaDescarcare(blob, numeAfisat);
  }

  // === Stări legale ===
  invalideaza(id: number, dto: InvalidareDispozitie): Promise<DispozitieDetalii> {
    return firstValueFrom(this.http.post<DispozitieDetalii>(`${this.urlBaza}/${id}/Invalidare`, dto));
  }

  anuleazaInvalidare(id: number): Promise<DispozitieDetalii> {
    return firstValueFrom(this.http.delete<DispozitieDetalii>(`${this.urlBaza}/${id}/Invalidare`));
  }

  // Individualele au override: confirmaPublicareIndividuala + motiv (ignorate la Normativ pe backend).
  publica(
    id: number, estePublicat: boolean,
    confirmaPublicareIndividuala = false, motiv: string | null = null
  ): Promise<DispozitieDetalii> {
    return firstValueFrom(this.http.put<DispozitieDetalii>(
      `${this.urlBaza}/${id}/Publicare`, { estePublicat, confirmaPublicareIndividuala, motiv }));
  }

  publicaMol(
    id: number, dataPublicareMol: string,
    confirmaPublicareIndividuala = false, motiv: string | null = null
  ): Promise<DispozitieDetalii> {
    return firstValueFrom(this.http.put<DispozitieDetalii>(
      `${this.urlBaza}/${id}/PublicareMol`, { dataPublicareMol, confirmaPublicareIndividuala, motiv }));
  }

  // „Anulează MOL" cere motiv obligatoriu → DELETE cu body (oglindă AnulareMolDispozitieDto).
  anuleazaMol(id: number, motiv: string): Promise<DispozitieDetalii> {
    return firstValueFrom(this.http.delete<DispozitieDetalii>(
      `${this.urlBaza}/${id}/PublicareMol`, { body: { motiv } }));
  }

  sterge(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.urlBaza}/${id}`));
  }

  // === Comunicări prefect (copie paralelă sub dispozitii/, §5 — sub-resursă self-contained) ===
  comunicari(id: number): Promise<ComunicareDispozitiePrefect[]> {
    return firstValueFrom(
      this.http.get<ComunicareDispozitiePrefect[]>(`${this.urlBaza}/${id}/Comunicari`));
  }

  adaugaComunicare(id: number, dto: CreareComunicare): Promise<ComunicareDispozitiePrefect> {
    return firstValueFrom(
      this.http.post<ComunicareDispozitiePrefect>(`${this.urlBaza}/${id}/Comunicari`, dto));
  }

  actualizeazaComunicare(
    id: number, comunicareId: number, dto: ActualizareComunicare
  ): Promise<ComunicareDispozitiePrefect> {
    return firstValueFrom(this.http.put<ComunicareDispozitiePrefect>(
      `${this.urlBaza}/${id}/Comunicari/${comunicareId}`, dto));
  }

  stergeComunicare(id: number, comunicareId: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(
      `${this.urlBaza}/${id}/Comunicari/${comunicareId}`));
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
