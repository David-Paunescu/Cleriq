import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EditareProcesVerbal, ProcesVerbal } from './proces-verbal.models';

@Injectable({ providedIn: 'root' })
export class ProcesVerbalService {
  private readonly http = inject(HttpClient);

  private url(sedintaId: number): string {
    return `${environment.apiUrl}/api/Sedinte/${sedintaId}/ProcesVerbal`;
  }

  obtine(sedintaId: number): Promise<ProcesVerbal> {
    return firstValueFrom(this.http.get<ProcesVerbal>(this.url(sedintaId)));
  }

  genereaza(sedintaId: number): Promise<ProcesVerbal> {
    return firstValueFrom(
      this.http.post<ProcesVerbal>(`${this.url(sedintaId)}/Genereaza`, {}));
  }

  editeaza(sedintaId: number, cerere: EditareProcesVerbal): Promise<ProcesVerbal> {
    return firstValueFrom(this.http.put<ProcesVerbal>(this.url(sedintaId), cerere));
  }

  finalizeaza(sedintaId: number): Promise<ProcesVerbal> {
    return firstValueFrom(
      this.http.post<ProcesVerbal>(`${this.url(sedintaId)}/Finalizeaza`, {}));
  }

  aproba(sedintaId: number, aprobatInSedintaId: number): Promise<ProcesVerbal> {
    return firstValueFrom(
      this.http.post<ProcesVerbal>(`${this.url(sedintaId)}/Aproba`, { aprobatInSedintaId }));
  }

  dezaproba(sedintaId: number): Promise<ProcesVerbal> {
    return firstValueFrom(
      this.http.delete<ProcesVerbal>(`${this.url(sedintaId)}/Aproba`));
  }

  async descarcaPdf(sedintaId: number, numeAfisat: string): Promise<void> {
    const blob = await firstValueFrom(
      this.http.get(`${this.url(sedintaId)}/Pdf`, { responseType: 'blob' }));
    this.declanseazaDescarcare(blob, numeAfisat);
  }

  incarcaSemnat(sedintaId: number, fisier: File): Promise<ProcesVerbal> {
    const form = new FormData();
    form.append('fisier', fisier);
    return firstValueFrom(
      this.http.post<ProcesVerbal>(`${this.url(sedintaId)}/Semnat`, form));
  }

  async descarcaSemnat(sedintaId: number, numeAfisat: string): Promise<void> {
    const blob = await firstValueFrom(
      this.http.get(`${this.url(sedintaId)}/Semnat`, { responseType: 'blob' }));
    this.declanseazaDescarcare(blob, numeAfisat);
  }

  stergeSemnat(sedintaId: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.url(sedintaId)}/Semnat`));
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