import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ActualizarePunct, CrearePunct, PunctOrdineZi, RezultatVot
} from './puncte.models';

@Injectable({ providedIn: 'root' })
export class PuncteService {
  private readonly http = inject(HttpClient);

  private url(sedintaId: number): string {
    return `${environment.apiUrl}/api/Sedinte/${sedintaId}/Puncte`;
  }

  lista(sedintaId: number): Promise<PunctOrdineZi[]> {
    return firstValueFrom(this.http.get<PunctOrdineZi[]>(this.url(sedintaId)));
  }

  creeaza(sedintaId: number, cerere: CrearePunct): Promise<PunctOrdineZi> {
    return firstValueFrom(this.http.post<PunctOrdineZi>(this.url(sedintaId), cerere));
  }

  actualizeaza(sedintaId: number, punctId: number, cerere: ActualizarePunct): Promise<PunctOrdineZi> {
    return firstValueFrom(
      this.http.put<PunctOrdineZi>(`${this.url(sedintaId)}/${punctId}`, cerere));
  }

  sterge(sedintaId: number, punctId: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.url(sedintaId)}/${punctId}`));
  }

  inchideVot(sedintaId: number, punctId: number): Promise<RezultatVot> {
    return firstValueFrom(
      this.http.post<RezultatVot>(`${this.url(sedintaId)}/${punctId}/Inchide`, {}));
  }

  amana(sedintaId: number, punctId: number): Promise<PunctOrdineZi> {
    return firstValueFrom(
      this.http.post<PunctOrdineZi>(`${this.url(sedintaId)}/${punctId}/Amana`, {}));
  }

  retrage(sedintaId: number, punctId: number): Promise<PunctOrdineZi> {
    return firstValueFrom(
      this.http.post<PunctOrdineZi>(`${this.url(sedintaId)}/${punctId}/Retrage`, {}));
  }
}