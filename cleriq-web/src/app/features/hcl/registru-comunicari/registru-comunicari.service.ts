import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { RegistruComunicare } from '../hcl.models';

export interface FiltreRegistru {
  an?: number;
  page?: number;
  size?: number;
}

@Injectable({ providedIn: 'root' })
export class RegistruComunicariService {
  private readonly http = inject(HttpClient);
  private readonly urlBaza = `${environment.apiUrl}/api/RegistruComunicariPrefect`;

  lista(filtre: FiltreRegistru = {}): Promise<RegistruComunicare[]> {
    let params = new HttpParams();
    if (filtre.an != null) params = params.set('an', filtre.an);
    if (filtre.page != null) params = params.set('page', filtre.page);
    if (filtre.size != null) params = params.set('size', filtre.size);
    return firstValueFrom(this.http.get<RegistruComunicare[]>(this.urlBaza, { params }));
  }
}
