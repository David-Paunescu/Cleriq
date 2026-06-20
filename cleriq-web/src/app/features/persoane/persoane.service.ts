import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ActualizarePersoana, CrearePersoana, Persoana } from './persoane.models';

@Injectable({ providedIn: 'root' })
export class PersoaneService {
  private readonly http = inject(HttpClient);
  private readonly urlBaza = `${environment.apiUrl}/api/Persoane`;

  lista(): Promise<Persoana[]> {
    return firstValueFrom(this.http.get<Persoana[]>(this.urlBaza));
  }

  creeaza(cerere: CrearePersoana): Promise<Persoana> {
    return firstValueFrom(this.http.post<Persoana>(this.urlBaza, cerere));
  }

  actualizeaza(id: number, cerere: ActualizarePersoana): Promise<Persoana> {
    return firstValueFrom(this.http.put<Persoana>(`${this.urlBaza}/${id}`, cerere));
  }

  sterge(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.urlBaza}/${id}`));
  }
}