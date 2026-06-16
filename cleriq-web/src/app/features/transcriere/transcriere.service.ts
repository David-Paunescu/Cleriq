import { HttpClient, HttpEvent, HttpEventType, HttpRequest } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { EditareTranscriere, Transcriere, TranscriereContinut } from './transcriere.models';

export interface ProgresUpload {
  procentaj: number;
  octetiTotali: number;
  octetiIncarcati: number;
  rezultat: Transcriere | null;
}

@Injectable({ providedIn: 'root' })
export class TranscriereService {
  private readonly http = inject(HttpClient);

  private url(sedintaId: number): string {
    return `${environment.apiUrl}/api/Sedinte/${sedintaId}/Transcriere`;
  }

  detalii(sedintaId: number): Promise<Transcriere> {
    return firstValueFrom(this.http.get<Transcriere>(this.url(sedintaId)));
  }

  obtineContinut(sedintaId: number): Promise<TranscriereContinut> {
    return firstValueFrom(
      this.http.get<TranscriereContinut>(`${this.url(sedintaId)}/Continut`));
  }

  editeazaContinut(sedintaId: number, cerere: EditareTranscriere): Promise<Transcriere> {
    return firstValueFrom(
      this.http.put<Transcriere>(`${this.url(sedintaId)}/Continut`, cerere));
  }

  publica(sedintaId: number): Promise<Transcriere> {
    return firstValueFrom(
      this.http.post<Transcriere>(`${this.url(sedintaId)}/Publica`, {}));
  }

  retragePublicare(sedintaId: number): Promise<Transcriere> {
    return firstValueFrom(
      this.http.delete<Transcriere>(`${this.url(sedintaId)}/Publica`));
  }

  retry(sedintaId: number): Promise<Transcriere> {
    return firstValueFrom(
      this.http.post<Transcriere>(`${this.url(sedintaId)}/Retry`, {}));
  }

  sterge(sedintaId: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(this.url(sedintaId)));
  }

  incarca(sedintaId: number, fisier: File): Observable<ProgresUpload> {
    const form = new FormData();
    form.append('fisier', fisier);

    const cerere = new HttpRequest('POST', this.url(sedintaId), form, {
      reportProgress: true
    });

    return new Observable<ProgresUpload>(observer => {
      const subscriere = this.http.request<Transcriere>(cerere).subscribe({
        next: (event: HttpEvent<Transcriere>) => {
          if (event.type === HttpEventType.UploadProgress) {
            const octetiTotali = event.total ?? fisier.size;
            observer.next({
              procentaj: Math.round((event.loaded / octetiTotali) * 100),
              octetiTotali,
              octetiIncarcati: event.loaded,
              rezultat: null
            });
          } else if (event.type === HttpEventType.Response && event.body) {
            observer.next({
              procentaj: 100,
              octetiTotali: fisier.size,
              octetiIncarcati: fisier.size,
              rezultat: event.body
            });
            observer.complete();
          }
        },
        error: (err) => observer.error(err)
      });

      return () => subscriere.unsubscribe();
    });
  }

  async descarcaAudio(sedintaId: number, numeAfisat: string): Promise<void> {
    const blob = await firstValueFrom(
      this.http.get(`${this.url(sedintaId)}/Audio`, { responseType: 'blob' }));
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = numeAfisat;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }

  obtineBlobAudio(sedintaId: number): Promise<Blob> {
    return firstValueFrom(
      this.http.get(`${this.url(sedintaId)}/Audio`, { responseType: 'blob' }));
  }
}