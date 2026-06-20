import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/auth/auth.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { ConfirmareDialog, DateConfirmare } from '../../../shared/confirmare/confirmare-dialog';
import { etichetaRolComisie } from '../../../shared/etichete';
import { ComisieDialog, DateComisieDialog } from '../comisie-dialog/comisie-dialog';
import { CorectareDataDialog, DateCorectareDataDialog } from '../corectare-data-dialog/corectare-data-dialog';
import { InchideMembruDialog, DateInchideMembruDialog } from '../inchide-membru-dialog/inchide-membru-dialog';
import { MembruDialog, DateMembruDialog } from '../membru-dialog/membru-dialog';
import { Comisie, MembruComisie } from '../comisii.models';
import { ComisiiService } from '../comisii.service';

const COLOANE_BAZA = ['consilier', 'rol', 'dataInceput', 'dataSfarsit'];

@Component({
  selector: 'app-comisie-detalii',
  imports: [
    MatCardModule, MatTableModule, MatIconModule, MatButtonModule,
    MatSlideToggleModule, MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './comisie-detalii.html',
  styleUrl: './comisie-detalii.scss'
})
export class ComisieDetalii {
  private readonly api = inject(ComisiiService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly id = Number(this.route.snapshot.paramMap.get('id'));

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly comisie = signal<Comisie | null>(null);
  readonly includeIstoric = signal(false);

  readonly esteAdmin = computed(() => this.auth.areRol('Admin'));

  readonly membriSortati = computed(() => {
    const c = this.comisie();
    if (!c) return [];
    return [...c.membri].sort((a, b) => b.dataInceput.localeCompare(a.dataInceput));
  });

  readonly coloane = computed(() =>
    this.esteAdmin() ? [...COLOANE_BAZA, 'actiuni'] : COLOANE_BAZA);

  readonly etichetaRol = etichetaRolComisie;

  constructor() {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      this.comisie.set(await this.api.detalii(this.id, this.includeIstoric()));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  async toggleIstoric(): Promise<void> {
    this.includeIstoric.update(v => !v);
    await this.incarca();
  }

  inapoiLaLista(): void {
    this.router.navigate(['/comisii']);
  }

  formateazaData(iso: string | null): string {
    if (!iso) return '—';
    const [an, luna, zi] = iso.split('-');
    return `${zi}.${luna}.${an}`;
  }

  async editeazaComisie(): Promise<void> {
    const c = this.comisie();
    if (!c) return;
    const data: DateComisieDialog = { comisie: c };
    const rezultat = await firstValueFrom(
      this.dialog.open(ComisieDialog, { data, width: '480px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Comisie actualizată.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  async stergeComisie(): Promise<void> {
    const c = this.comisie();
    if (!c) return;
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
      await this.api.sterge(this.id);
      this.snackBar.open('Comisie ștearsă.', 'Închide', { duration: 4000 });
      this.router.navigate(['/comisii']);
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }

  async adaugaMembru(): Promise<void> {
    const c = this.comisie();
    if (!c) return;

    const consilieriDejaActivi = c.membri
      .filter(m => m.dataSfarsit === null)
      .map(m => m.consilierId);

    const data: DateMembruDialog = {
      comisieId: this.id,
      consilieriDejaActivi
    };
    const rezultat = await firstValueFrom(
      this.dialog.open(MembruDialog, { data, width: '520px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Membru adăugat.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  async inchideMembrie(m: MembruComisie): Promise<void> {
    const data: DateInchideMembruDialog = {
      comisieId: this.id,
      consilierId: m.consilierId,
      numeComplet: m.numeComplet,
      dataInceput: m.dataInceput
    };
    const rezultat = await firstValueFrom(
      this.dialog.open(InchideMembruDialog, { data, width: '460px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Membru scos din comisie.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  async corecteazaDataInceput(m: MembruComisie): Promise<void> {
    const data: DateCorectareDataDialog = {
      comisieId: this.id,
      consilierId: m.consilierId,
      numeComplet: m.numeComplet,
      dataInceputCurenta: m.dataInceput,
      dataSfarsit: m.dataSfarsit
    };
    const rezultat = await firstValueFrom(
      this.dialog.open(CorectareDataDialog, { data, width: '460px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Data început actualizată.', 'Închide', { duration: 4000 });
    await this.incarca();
  }
}