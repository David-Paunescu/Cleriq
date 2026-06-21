import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MembruIstoric, SubiectIstoric, ViceprimarIstoric } from './functii-istorice.models';

@Injectable({ providedIn: 'root' })
export class FunctiiIstoriceService {
  private readonly http = inject(HttpClient);
  private readonly urlBaza = `${environment.apiUrl}/api/FunctiiIstorice`;

  primar(data: string): Promise<SubiectIstoric | null> {
    const params = new HttpParams().set('data', data);
    return firstValueFrom(
      this.http.get<SubiectIstoric | null>(`${this.urlBaza}/Primar`, { params }));
  }

  secretarUat(data: string): Promise<SubiectIstoric | null> {
    const params = new HttpParams().set('data', data);
    return firstValueFrom(
      this.http.get<SubiectIstoric | null>(`${this.urlBaza}/SecretarUat`, { params }));
  }

  viceprimari(data: string): Promise<ViceprimarIstoric[]> {
    const params = new HttpParams().set('data', data);
    return firstValueFrom(
      this.http.get<ViceprimarIstoric[]>(`${this.urlBaza}/Viceprimari`, { params }));
  }

  consilieri(data: string): Promise<SubiectIstoric[]> {
    const params = new HttpParams().set('data', data);
    return firstValueFrom(
      this.http.get<SubiectIstoric[]>(`${this.urlBaza}/Consilieri`, { params }));
  }

  membriComisie(comisieId: number, data: string): Promise<MembruIstoric[]> {
    const params = new HttpParams().set('data', data);
    return firstValueFrom(
      this.http.get<MembruIstoric[]>(
        `${this.urlBaza}/Comisii/${comisieId}/Membri`, { params }));
  }

  presedinteComisie(comisieId: number, data: string): Promise<SubiectIstoric | null> {
    const params = new HttpParams().set('data', data);
    return firstValueFrom(
      this.http.get<SubiectIstoric | null>(
        `${this.urlBaza}/Comisii/${comisieId}/Presedinte`, { params }));
  }
}