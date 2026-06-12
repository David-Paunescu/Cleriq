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
import { ConsilierDialog, DateConsilierDialog } from '../consilier-dialog/consilier-dialog';
import { ContDialog, DateContDialog } from '../cont-dialog/cont-dialog';
import { Consilier } from '../consilieri.models';
import { ConsilieriService } from '../consilieri.service';

const COLOANE_BAZA = ['numeComplet', 'email', 'telefon', 'activ', 'areCont'];

@Component({
  selector: 'app-consilieri-lista',
  imports: [
    MatTableModule, MatFormFieldModule, MatInputModule,
    MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './consilieri-lista.html',
  styleUrl: './consilieri-lista.scss'
})
export class ConsilieriLista {
  private readonly api = inject(ConsilieriService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly consilieri = signal<Consilier[]>([]);
  readonly filtru = signal('');

  readonly esteAdmin = computed(() => this.auth.areRol('Admin'));

  readonly consilieriFiltrati = computed(() => {
    const termen = normalizeazaPentruCautare(this.filtru());
    if (!termen) return this.consilieri();
    return this.consilieri().filter(c =>
      normalizeazaPentruCautare(c.numeComplet).includes(termen)
      || normalizeazaPentruCautare(c.email ?? '').includes(termen));
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
      this.consilieri.set(await this.api.lista());
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
    const data: DateConsilierDialog = {};
    const rezultat = await firstValueFrom(
      this.dialog.open(ConsilierDialog, { data, width: '480px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Consilier adăugat.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  async editeaza(consilier: Consilier): Promise<void> {
    const data: DateConsilierDialog = { consilier };
    const rezultat = await firstValueFrom(
      this.dialog.open(ConsilierDialog, { data, width: '480px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Consilier actualizat.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  async sterge(consilier: Consilier): Promise<void> {
    const data: DateConfirmare = {
      titlu: 'Ștergere consilier',
      mesaj: `Ștergi consilierul „${consilier.numeComplet}"? Acțiunea îl elimină din listele instituției.`,
      etichetaConfirmare: 'Șterge',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data, width: '420px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    try {
      await this.api.sterge(consilier.id);
      this.snackBar.open('Consilier șters.', 'Închide', { duration: 4000 });
      await this.incarca();
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }

  async deschideCont(consilier: Consilier): Promise<void> {
    const data: DateContDialog = { consilier };
    const rezultat = await firstValueFrom(
      this.dialog.open(ContDialog, { data, width: '480px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open(`Cont de acces creat pentru ${consilier.numeComplet}.`, 'Închide',
      { duration: 4000 });
    await this.incarca();
  }
}