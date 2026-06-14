import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  InregistrareVot, InregistrareVotSelf, Vot, VoturiPunct
} from './voturi.models';

@Injectable({ providedIn: 'root' })
export class VoturiService {
  private readonly http = inject(HttpClient);

  private url(sedintaId: number, punctId: number): string {
    return `${environment.apiUrl}/api/Sedinte/${sedintaId}/Puncte/${punctId}/Voturi`;
  }

  lista(sedintaId: number, punctId: number): Promise<VoturiPunct> {
    return firstValueFrom(this.http.get<VoturiPunct>(this.url(sedintaId, punctId)));
  }

  inregistreaza(sedintaId: number, punctId: number, cerere: InregistrareVot): Promise<Vot> {
    return firstValueFrom(this.http.post<Vot>(this.url(sedintaId, punctId), cerere));
  }

  inregistreazaSelf(sedintaId: number, punctId: number, cerere: InregistrareVotSelf): Promise<Vot> {
    return firstValueFrom(this.http.post<Vot>(`${this.url(sedintaId, punctId)}/Self`, cerere));
  }
}