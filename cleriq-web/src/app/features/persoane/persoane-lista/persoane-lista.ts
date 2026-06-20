import { Component, computed, inject, signal } from '@angular/core';
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
import { DatePersoanaDialog, PersoanaDialog } from '../persoana-dialog/persoana-dialog';
import { Persoana } from '../persoane.models';
import { actiuniPermise } from '../persoane.permisiuni';
import { PersoaneService } from '../persoane.service';

const COLOANE_BAZA = ['numeComplet', 'email', 'telefon', 'mandate'];

@Component({
  selector: 'app-persoane-lista',
  imports: [
    MatTableModule, MatFormFieldModule, MatInputModule,
    MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './persoane-lista.html',
  styleUrl: './persoane-lista.scss'
})
export class PersoaneLista {
  private readonly api = inject(PersoaneService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly persoane = signal<Persoana[]>([]);
  readonly filtru = signal('');

  readonly esteAdmin = computed(() => this.auth.areRol('Admin'));

  readonly persoaneFiltrate = computed(() => {
    const termen = normalizeazaPentruCautare(this.filtru());
    if (!termen) return this.persoane();
    return this.persoane().filter(p =>
      normalizeazaPentruCautare(p.numeComplet).includes(termen)
      || normalizeazaPentruCautare(p.email ?? '').includes(termen));
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
      this.persoane.set(await this.api.lista());
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
    const data: DatePersoanaDialog = {};
    const rezultat = await firstValueFrom(
      this.dialog.open(PersoanaDialog, { data, width: '480px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Persoană adăugată.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  async editeaza(persoana: Persoana): Promise<void> {
    const data: DatePersoanaDialog = { persoana };
    const rezultat = await firstValueFrom(
      this.dialog.open(PersoanaDialog, { data, width: '480px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Persoană actualizată.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  async sterge(persoana: Persoana): Promise<void> {
    const date: DateConfirmare = {
      titlu: 'Ștergere persoană',
      mesaj: `Ștergi persoana „${persoana.numeComplet}"? Acțiunea o elimină din listele instituției.`,
      etichetaConfirmare: 'Șterge',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '420px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    try {
      await this.api.sterge(persoana.id);
      this.snackBar.open('Persoană ștearsă.', 'Închide', { duration: 4000 });
      await this.incarca();
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }

  permisiuni(p: Persoana) {
    return actiuniPermise(p);
  }
}