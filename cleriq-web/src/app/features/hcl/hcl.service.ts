import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { StatusHclRedactional, TipHcl } from '../../shared/enums';
import {
  ActualizareComunicare, AdaugareSemnatar, AtribuireNumarHcl, ComunicareHclPrefect, CreareComunicare,
  CreareHcl, CreareRelatie, EditareContinutHcl, Hcl, HclDetalii, InvalidareHcl, RelatieHcl, RelatiiHcl,
  SugestieNumar
} from './hcl.models';

export interface FiltreHcl {
  an?: number;
  status?: StatusHclRedactional;
  tipHcl?: TipHcl;
  skip?: number;
  take?: number;
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
    if (filtre.skip != null) params = params.set('skip', filtre.skip);
    if (filtre.take != null) params = params.set('take', filtre.take);
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

  // === FE2 — semnatari (POST/DELETE întorc HclDetalii: garda „Semnează" depinde de listă) ===
  adaugaSemnatar(id: number, dto: AdaugareSemnatar): Promise<HclDetalii> {
    return firstValueFrom(this.http.post<HclDetalii>(`${this.urlBaza}/${id}/Semnatari`, dto));
  }

  stergeSemnatar(id: number, semnatarId: number): Promise<HclDetalii> {
    return firstValueFrom(
      this.http.delete<HclDetalii>(`${this.urlBaza}/${id}/Semnatari/${semnatarId}`));
  }

  seteazaMotivLipsa(id: number, motiv: string): Promise<HclDetalii> {
    return firstValueFrom(
      this.http.put<HclDetalii>(`${this.urlBaza}/${id}/MotivLipsaPresedinte`, { motiv }));
  }

  // === FE2 — stări legale ===
  publica(id: number, estePublicat: boolean): Promise<HclDetalii> {
    return firstValueFrom(
      this.http.put<HclDetalii>(`${this.urlBaza}/${id}/Publicare`, { estePublicat }));
  }

  publicaMol(id: number, dataPublicareMol: string): Promise<HclDetalii> {
    return firstValueFrom(
      this.http.put<HclDetalii>(`${this.urlBaza}/${id}/PublicareMol`, { dataPublicareMol }));
  }

  // „Anulează MOL" cere motiv obligatoriu → DELETE cu body (oglindă AnulareMolDto).
  anuleazaMol(id: number, motiv: string): Promise<HclDetalii> {
    return firstValueFrom(
      this.http.delete<HclDetalii>(`${this.urlBaza}/${id}/PublicareMol`, { body: { motiv } }));
  }

  invalideaza(id: number, dto: InvalidareHcl): Promise<HclDetalii> {
    return firstValueFrom(this.http.post<HclDetalii>(`${this.urlBaza}/${id}/Invalidare`, dto));
  }

  anuleazaInvalidare(id: number): Promise<HclDetalii> {
    return firstValueFrom(this.http.delete<HclDetalii>(`${this.urlBaza}/${id}/Invalidare`));
  }

  // === FE2 — variantă semnată (PDF scanat) ===
  incarcaSemnat(id: number, fisier: File): Promise<HclDetalii> {
    const form = new FormData();
    form.append('fisier', fisier);
    return firstValueFrom(this.http.post<HclDetalii>(`${this.urlBaza}/${id}/Semnat`, form));
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

  // === FE3 — comunicări prefect (sub-resursă self-contained: întoarce DTO-ul propriu) ===
  comunicari(hclId: number): Promise<ComunicareHclPrefect[]> {
    return firstValueFrom(
      this.http.get<ComunicareHclPrefect[]>(`${this.urlBaza}/${hclId}/Comunicari`));
  }

  adaugaComunicare(hclId: number, dto: CreareComunicare): Promise<ComunicareHclPrefect> {
    return firstValueFrom(
      this.http.post<ComunicareHclPrefect>(`${this.urlBaza}/${hclId}/Comunicari`, dto));
  }

  actualizeazaComunicare(
    hclId: number, comunicareId: number, dto: ActualizareComunicare
  ): Promise<ComunicareHclPrefect> {
    return firstValueFrom(this.http.put<ComunicareHclPrefect>(
      `${this.urlBaza}/${hclId}/Comunicari/${comunicareId}`, dto));
  }

  stergeComunicare(hclId: number, comunicareId: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(
      `${this.urlBaza}/${hclId}/Comunicari/${comunicareId}`));
  }

  // === FE3 — relații cu alte acte (self-contained) ===
  relatii(hclId: number): Promise<RelatiiHcl> {
    return firstValueFrom(this.http.get<RelatiiHcl>(`${this.urlBaza}/${hclId}/Relatii`));
  }

  adaugaRelatie(hclId: number, dto: CreareRelatie): Promise<RelatieHcl> {
    return firstValueFrom(this.http.post<RelatieHcl>(`${this.urlBaza}/${hclId}/Relatii`, dto));
  }

  stergeRelatie(hclId: number, relatieId: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.urlBaza}/${hclId}/Relatii/${relatieId}`));
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
