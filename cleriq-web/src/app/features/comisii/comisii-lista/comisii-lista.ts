import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/auth/auth.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { ConfirmareDialog, DateConfirmare } from '../../../shared/confirmare/confirmare-dialog';
import { normalizeazaPentruCautare } from '../../../shared/text';
import { ComisieDialog, DateComisieDialog } from '../comisie-dialog/comisie-dialog';
import { Comisie } from '../comisii.models';
import { ComisiiService } from '../comisii.service';

const COLOANE_BAZA = ['denumire', 'descriere'];

@Component({
  selector: 'app-comisii-lista',
  imports: [
    MatTableModule, MatFormFieldModule, MatInputModule,
    MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './comisii-lista.html',
  styleUrl: './comisii-lista.scss'
})
export class ComisiiLista {
  private readonly api = inject(ComisiiService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly comisii = signal<Comisie[]>([]);
  readonly filtru = signal('');

  readonly esteAdmin = computed(() => this.auth.areRol('Admin'));

  readonly comisiiFiltrate = computed(() => {
    const termen = normalizeazaPentruCautare(this.filtru());
    if (!termen) return this.comisii();
    return this.comisii().filter(c =>
      normalizeazaPentruCautare(c.denumire).includes(termen)
      || normalizeazaPentruCautare(c.descriere ?? '').includes(termen));
  });

  readonly coloane = computed(() =>
    this.esteAdmin() ? [...COLOANE_BAZA, 'actiuni'] : COLOANE_BAZA);

  constructor() {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      this.comisii.set(await this.api.lista());
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  laCautare(event: Event): void {
    this.filtru.set((event.target as HTMLInputElement).value);
  }

  async adauga(): Promise<void> {
    const data: DateComisieDialog = {};
    const rezultat = await firstValueFrom(
      this.dialog.open(ComisieDialog, { data, width: '480px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Comisie adăugată.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  deschide(c: Comisie): void {
    this.router.navigate(['/comisii', c.id]);
  }

  async editeaza(c: Comisie, event: Event): Promise<void> {
    event.stopPropagation();
    const data: DateComisieDialog = { comisie: c };
    const rezultat = await firstValueFrom(
      this.dialog.open(ComisieDialog, { data, width: '480px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Comisie actualizată.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  async sterge(c: Comisie, event: Event): Promise<void> {
    event.stopPropagation();

    const date: DateConfirmare = {
      titlu: 'Ștergere comisie',
      mesaj: `Ștergi comisia „${c.denumire}"?`,
      etichetaConfirmare: 'Șterge',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '420px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    try {
      await this.api.sterge(c.id);
      this.snackBar.open('Comisie ștearsă.', 'Închide', { duration: 4000 });
      await this.incarca();
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }
}