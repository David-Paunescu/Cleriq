import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ActualizareConsilier, Consilier, ContConsilier,
  CreareConsilier, CreareContConsilier
} from './consilieri.models';

@Injectable({ providedIn: 'root' })
export class ConsilieriService {
  private readonly http = inject(HttpClient);
  private readonly urlBaza = `${environment.apiUrl}/api/Consilieri`;

  lista(): Promise<Consilier[]> {
    return firstValueFrom(this.http.get<Consilier[]>(this.urlBaza));
  }

  creeaza(cerere: CreareConsilier): Promise<Consilier> {
    return firstValueFrom(this.http.post<Consilier>(this.urlBaza, cerere));
  }

  actualizeaza(id: number, cerere: ActualizareConsilier): Promise<Consilier> {
    return firstValueFrom(this.http.put<Consilier>(`${this.urlBaza}/${id}`, cerere));
  }

  sterge(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.urlBaza}/${id}`));
  }

  creeazaCont(consilierId: number, cerere: CreareContConsilier): Promise<ContConsilier> {
    return firstValueFrom(
      this.http.post<ContConsilier>(`${this.urlBaza}/${consilierId}/Cont`, cerere));
  }
}