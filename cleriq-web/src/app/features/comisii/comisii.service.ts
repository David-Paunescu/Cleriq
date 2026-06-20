import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ActualizareComisie, ActualizareDataInceputMembru, AdaugareMembru,
  Comisie, CreareComisie, MembruComisie
} from './comisii.models';

@Injectable({ providedIn: 'root' })
export class ComisiiService {
  private readonly http = inject(HttpClient);
  private readonly urlBaza = `${environment.apiUrl}/api/Comisii`;

  lista(): Promise<Comisie[]> {
    return firstValueFrom(this.http.get<Comisie[]>(this.urlBaza));
  }

  detalii(id: number, includeIstoric = false): Promise<Comisie> {
    const params = new HttpParams().set('includeIstoric', includeIstoric);
    return firstValueFrom(this.http.get<Comisie>(`${this.urlBaza}/${id}`, { params }));
  }

  creeaza(cerere: CreareComisie): Promise<Comisie> {
    return firstValueFrom(this.http.post<Comisie>(this.urlBaza, cerere));
  }

  actualizeaza(id: number, cerere: ActualizareComisie): Promise<Comisie> {
    return firstValueFrom(this.http.put<Comisie>(`${this.urlBaza}/${id}`, cerere));
  }

  sterge(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.urlBaza}/${id}`));
  }

  adaugaMembru(comisieId: number, cerere: AdaugareMembru): Promise<MembruComisie> {
    return firstValueFrom(
      this.http.post<MembruComisie>(`${this.urlBaza}/${comisieId}/Membri`, cerere));
  }

  scoateMembru(comisieId: number, consilierId: number, dataSfarsit: string): Promise<void> {
    const params = new HttpParams().set('dataSfarsit', dataSfarsit);
    return firstValueFrom(
      this.http.delete<void>(
        `${this.urlBaza}/${comisieId}/Membri/${consilierId}`, { params }));
  }

  actualizeazaDataInceputMembru(
    comisieId: number, consilierId: number, cerere: ActualizareDataInceputMembru
  ): Promise<MembruComisie> {
    return firstValueFrom(
      this.http.put<MembruComisie>(
        `${this.urlBaza}/${comisieId}/Membri/${consilierId}/DataInceput`, cerere));
  }
}