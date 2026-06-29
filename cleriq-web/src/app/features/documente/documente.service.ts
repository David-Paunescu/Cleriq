import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TipDocument, TipDocumentHcl } from '../../shared/enums';
import {
  ActualizareDocument, Document, SetareVizibilitate
} from './documente.models';

export interface ContextDocument {
  sedintaId?: number;
  punctId?: number;
  hclId?: number;
}

export interface DateUpload {
  fisier: File;
  denumire: string;
  tipDocument: TipDocument;
  descriere: string | null;
  ordine: number;
  context: ContextDocument;
  tipDocumentHcl?: TipDocumentHcl | null;
  numarOrdinAnexa?: number | null;
}

@Injectable({ providedIn: 'root' })
export class DocumenteService {
  private readonly http = inject(HttpClient);
  private readonly urlBaza = `${environment.apiUrl}/api/Documente`;

  lista(context: ContextDocument): Promise<Document[]> {
    let params = new HttpParams();
    if (context.sedintaId != null) params = params.set('sedintaId', context.sedintaId);
    if (context.punctId != null) params = params.set('punctId', context.punctId);
    if (context.hclId != null) params = params.set('hclId', context.hclId);
    return firstValueFrom(this.http.get<Document[]>(this.urlBaza, { params }));
  }

  incarca(date: DateUpload): Promise<Document> {
    const form = new FormData();
    form.append('fisier', date.fisier);
    form.append('denumire', date.denumire);
    form.append('tipDocument', date.tipDocument.toString());
    if (date.descriere) form.append('descriere', date.descriere);
    form.append('ordine', date.ordine.toString());
    if (date.context.sedintaId != null)
      form.append('sedintaId', date.context.sedintaId.toString());
    if (date.context.punctId != null)
      form.append('punctId', date.context.punctId.toString());
    if (date.context.hclId != null)
      form.append('hclId', date.context.hclId.toString());
    if (date.tipDocumentHcl != null)
      form.append('tipDocumentHcl', date.tipDocumentHcl.toString());
    if (date.numarOrdinAnexa != null)
      form.append('numarOrdinAnexa', date.numarOrdinAnexa.toString());

    return firstValueFrom(this.http.post<Document>(this.urlBaza, form));
  }

  actualizeaza(id: number, cerere: ActualizareDocument): Promise<Document> {
    return firstValueFrom(this.http.put<Document>(`${this.urlBaza}/${id}`, cerere));
  }

  seteazaVizibilitate(id: number, cerere: SetareVizibilitate): Promise<Document> {
    return firstValueFrom(
      this.http.put<Document>(`${this.urlBaza}/${id}/Vizibilitate`, cerere));
  }

  sterge(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.urlBaza}/${id}`));
  }

  async descarca(id: number, numeFisier: string): Promise<void> {
    const blob = await firstValueFrom(
      this.http.get(`${this.urlBaza}/${id}/Continut`, { responseType: 'blob' }));
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = numeFisier;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
}