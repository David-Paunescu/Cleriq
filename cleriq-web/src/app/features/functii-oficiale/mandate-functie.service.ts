import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TipFunctie } from '../../shared/enums';
import {
  ActualizareMandatFunctie, CreareMandatFunctie,
  InchideMandatFunctie, MandatFunctie
} from './mandate-functie.models';

@Injectable({ providedIn: 'root' })
export class MandateFunctieService {
  private readonly http = inject(HttpClient);
  private readonly urlBaza = `${environment.apiUrl}/api/MandateFunctie`;

  lista(tipFunctie?: TipFunctie): Promise<MandatFunctie[]> {
    let params = new HttpParams();
    if (tipFunctie != null) params = params.set('tipFunctie', tipFunctie);
    return firstValueFrom(this.http.get<MandatFunctie[]>(this.urlBaza, { params }));
  }

  creeaza(cerere: CreareMandatFunctie): Promise<MandatFunctie> {
    return firstValueFrom(this.http.post<MandatFunctie>(this.urlBaza, cerere));
  }

  actualizeaza(id: number, cerere: ActualizareMandatFunctie): Promise<MandatFunctie> {
    return firstValueFrom(this.http.put<MandatFunctie>(`${this.urlBaza}/${id}`, cerere));
  }

  inchide(id: number, cerere: InchideMandatFunctie): Promise<MandatFunctie> {
    return firstValueFrom(
      this.http.post<MandatFunctie>(`${this.urlBaza}/${id}/Inchide`, cerere));
  }
}