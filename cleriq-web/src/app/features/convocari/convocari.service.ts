import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CanalNotificare } from '../../shared/enums';
import { Convocare, IncercareTrimitere, RezultatConvocare } from './convocari.models';

@Injectable({ providedIn: 'root' })
export class ConvocariService {
  private readonly http = inject(HttpClient);

  private url(sedintaId: number): string {
    return `${environment.apiUrl}/api/Sedinte/${sedintaId}`;
  }

  trimite(sedintaId: number): Promise<RezultatConvocare> {
    return firstValueFrom(
      this.http.post<RezultatConvocare>(`${this.url(sedintaId)}/Convocare`, {}));
  }

  lista(sedintaId: number): Promise<Convocare[]> {
    return firstValueFrom(this.http.get<Convocare[]>(`${this.url(sedintaId)}/Convocari`));
  }

  listaIncercari(
    sedintaId: number, convocareId: number, canal?: CanalNotificare
  ): Promise<IncercareTrimitere[]> {
    let params = new HttpParams();
    if (canal != null) params = params.set('canal', canal);
    return firstValueFrom(this.http.get<IncercareTrimitere[]>(
      `${this.url(sedintaId)}/Convocari/${convocareId}/Incercari`, { params }));
  }

  retry(sedintaId: number, convocareId: number): Promise<Convocare> {
    return firstValueFrom(this.http.post<Convocare>(
      `${this.url(sedintaId)}/Convocari/${convocareId}/Retry`, {}));
  }

  reseteaza(sedintaId: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.url(sedintaId)}/Convocare`));
  }
}