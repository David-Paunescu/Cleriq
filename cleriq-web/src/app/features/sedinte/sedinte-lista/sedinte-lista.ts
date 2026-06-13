import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/auth/auth.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { ConfirmareDialog, DateConfirmare } from '../../../shared/confirmare/confirmare-dialog';
import { formateazaDataScurta } from '../../../shared/data';
import { StatusSedinta } from '../../../shared/enums';
import { etichetaModDesfasurare, etichetaStatusSedinta, etichetaTipSedinta } from '../../../shared/etichete';
import { normalizeazaPentruCautare } from '../../../shared/text';
import { actiuniPermise } from '../sedinte.permisiuni';
import { Sedinta } from '../sedinte.models';
import { SedinteService } from '../sedinte.service';

const COLOANE_BAZA = ['titlu', 'dataOra', 'tip', 'modDesfasurare', 'status'];

@Component({
  selector: 'app-sedinte-lista',
  imports: [
    MatTableModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './sedinte-lista.html',
  styleUrl: './sedinte-lista.scss'
})
export class SedinteLista {
  private readonly api = inject(SedinteService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly sedinte = signal<Sedinta[]>([]);
  readonly filtru = signal('');
  readonly statusFiltru = signal<StatusSedinta | 'toate'>('toate');

  readonly StatusSedinta = StatusSedinta;
  readonly statusuri: StatusSedinta[] = [
    StatusSedinta.Planificata,
    StatusSedinta.Convocata,
    StatusSedinta.InDesfasurare,
    StatusSedinta.Finalizata,
    StatusSedinta.Anulata
  ];

  readonly poateScrie = computed(() => this.auth.areOricareRol('Admin', 'Secretar'));
  readonly poateSterge = computed(() => this.auth.areRol('Admin'));

  readonly sedinteFiltrate = computed(() => {
    const termen = normalizeazaPentruCautare(this.filtru());
    const status = this.statusFiltru();
    return this.sedinte()
      .filter(s => status === 'toate' || s.status === status)
      .filter(s => !termen
        || normalizeazaPentruCautare(s.titlu).includes(termen)
        || normalizeazaPentruCautare(s.numar ?? '').includes(termen)
        || normalizeazaPentruCautare(s.loc ?? '').includes(termen))
      .sort((a, b) => b.dataOra.localeCompare(a.dataOra));
  });

  readonly coloane = computed(() =>
    this.poateScrie() ? [...COLOANE_BAZA, 'actiuni'] : COLOANE_BAZA);

  constructor() {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      this.sedinte.set(await this.api.lista());
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  laCautare(event: Event): void {
    this.filtru.set((event.target as HTMLInputElement).value);
  }

  laStatusSchimbat(valoare: StatusSedinta | 'toate'): void {
    this.statusFiltru.set(valoare);
  }

  adauga(): void {
    this.router.navigate(['/sedinte/noua']);
  }

  deschide(s: Sedinta): void {
    this.router.navigate(['/sedinte', s.id]);
  }

  editeaza(s: Sedinta, event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/sedinte', s.id, 'editeaza']);
  }

  async sterge(s: Sedinta, event: Event): Promise<void> {
    event.stopPropagation();

    const date: DateConfirmare = {
      titlu: 'Ștergere ședință',
      mesaj: `Ștergi ședința „${s.titlu}"?`,
      etichetaConfirmare: 'Șterge',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '420px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    try {
      await this.api.sterge(s.id);
      this.snackBar.open('Ședință ștearsă.', 'Închide', { duration: 4000 });
      await this.incarca();
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }

  permisiuni(s: Sedinta) {
    return actiuniPermise(s.status);
  }

  formateaza = formateazaDataScurta;
  etichetaTip = etichetaTipSedinta;
  etichetaMod = etichetaModDesfasurare;
  etichetaStatus = etichetaStatusSedinta;

  claseStatus(status: StatusSedinta): string {
    switch (status) {
      case StatusSedinta.Planificata: return 'badge badge-planificata';
      case StatusSedinta.Convocata: return 'badge badge-convocata';
      case StatusSedinta.InDesfasurare: return 'badge badge-indesfasurare';
      case StatusSedinta.Finalizata: return 'badge badge-finalizata';
      case StatusSedinta.Anulata: return 'badge badge-anulata';
    }
  }
}