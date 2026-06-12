import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-acasa',
  imports: [MatCardModule, MatButtonModule],
  templateUrl: './acasa.html'
})
export class Acasa {
  private readonly http = inject(HttpClient);

  readonly auth = inject(AuthService);
  readonly numarConsilieri = signal<number | null>(null);

  constructor() {
    this.incarcaConsilieri();
  }

  private async incarcaConsilieri(): Promise<void> {
    try {
      const consilieri = await firstValueFrom(
        this.http.get<unknown[]>(`${environment.apiUrl}/api/Consilieri`));
      this.numarConsilieri.set(consilieri.length);
    } catch {
      // interceptorul tratează transversalele; placeholder-ul rămâne fără număr
    }
  }
}