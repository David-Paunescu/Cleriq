import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { HclUrgent } from './hcl.models';

@Injectable({ providedIn: 'root' })
export class HclDashboardService {
  private readonly http = inject(HttpClient);
  private readonly urlBaza = `${environment.apiUrl}/api/Hcl`;

  // [Admin,Secretar] — apelat doar din widgetul de pe Acasă, randat condiționat pe rol.
  urgentDeComunicat(prag = 3): Promise<HclUrgent[]> {
    const params = new HttpParams().set('prag', prag);
    return firstValueFrom(
      this.http.get<HclUrgent[]>(`${this.urlBaza}/UrgentDeComunicat`, { params }));
  }
}
