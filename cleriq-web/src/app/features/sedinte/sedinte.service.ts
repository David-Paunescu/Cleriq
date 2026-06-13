import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ActualizareSedinta, CreareSedinta, Sedinta } from './sedinte.models';

@Injectable({ providedIn: 'root' })
export class SedinteService {
  private readonly http = inject(HttpClient);
  private readonly urlBaza = `${environment.apiUrl}/api/Sedinte`;

  lista(): Promise<Sedinta[]> {
    return firstValueFrom(this.http.get<Sedinta[]>(this.urlBaza));
  }

  detalii(id: number): Promise<Sedinta> {
    return firstValueFrom(this.http.get<Sedinta>(`${this.urlBaza}/${id}`));
  }

  creeaza(cerere: CreareSedinta): Promise<Sedinta> {
    return firstValueFrom(this.http.post<Sedinta>(this.urlBaza, cerere));
  }

  actualizeaza(id: number, cerere: ActualizareSedinta): Promise<Sedinta> {
    return firstValueFrom(this.http.put<Sedinta>(`${this.urlBaza}/${id}`, cerere));
  }

  sterge(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.urlBaza}/${id}`));
  }

  incepe(id: number): Promise<Sedinta> {
    return firstValueFrom(this.http.post<Sedinta>(`${this.urlBaza}/${id}/Incepe`, {}));
  }

  finalizeaza(id: number): Promise<Sedinta> {
    return firstValueFrom(this.http.post<Sedinta>(`${this.urlBaza}/${id}/Finalizeaza`, {}));
  }

  anuleaza(id: number): Promise<Sedinta> {
    return firstValueFrom(this.http.post<Sedinta>(`${this.urlBaza}/${id}/Anuleaza`, {}));
  }
}