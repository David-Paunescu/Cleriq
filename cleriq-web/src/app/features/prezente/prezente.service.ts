import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Prezenta, SetarePrezenta } from './prezente.models';

@Injectable({ providedIn: 'root' })
export class PrezenteService {
  private readonly http = inject(HttpClient);

  private url(sedintaId: number): string {
    return `${environment.apiUrl}/api/Sedinte/${sedintaId}/Prezente`;
  }

  lista(sedintaId: number): Promise<Prezenta[]> {
    return firstValueFrom(this.http.get<Prezenta[]>(this.url(sedintaId)));
  }

  seteaza(sedintaId: number, cerere: SetarePrezenta): Promise<Prezenta> {
    return firstValueFrom(this.http.post<Prezenta>(this.url(sedintaId), cerere));
  }
}