import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/auth/auth.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { ConfirmareDialog, DateConfirmare } from '../../../shared/confirmare/confirmare-dialog';
import { formateazaDataOra, indicatorFus } from '../../../shared/data';
import { StatusSedinta } from '../../../shared/enums';
import { etichetaModDesfasurare, etichetaStatusSedinta, etichetaTipSedinta } from '../../../shared/etichete';
import { actiuniPermise } from '../sedinte.permisiuni';
import { Sedinta } from '../sedinte.models';
import { SedinteService } from '../sedinte.service';
import { PuncteTab } from '../../puncte/puncte-tab/puncte-tab';
import { PrezentaTab } from '../../prezente/prezenta-tab/prezenta-tab';
import { VoturiTab } from '../../voturi/voturi-tab/voturi-tab';
import { DocumenteTab } from '../../documente/documente-tab/documente-tab';
import { ConvocariTab } from '../../convocari/convocari-tab/convocari-tab';

@Component({
  selector: 'app-sedinta-detalii',
  imports: [
    MatCardModule, MatTabsModule, MatIconModule, MatButtonModule,
    MatMenuModule, MatTooltipModule, MatProgressSpinnerModule,
    PuncteTab, PrezentaTab, VoturiTab, DocumenteTab, ConvocariTab
  ],
  templateUrl: './sedinta-detalii.html',
  styleUrl: './sedinta-detalii.scss'
})
export class SedintaDetalii {
  private readonly api = inject(SedinteService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly id = Number(this.route.snapshot.paramMap.get('id'));

  readonly seIncarca = signal(false);
  readonly seExecuta = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly sedinta = signal<Sedinta | null>(null);

  readonly poateScrie = computed(() => this.auth.areOricareRol('Admin', 'Secretar'));
  readonly poateSterge = computed(() => this.auth.areRol('Admin'));

  readonly permisiuni = computed(() => {
    const s = this.sedinta();
    return s ? actiuniPermise(s.status) : null;
  });

  readonly formateazaData = formateazaDataOra;
  readonly indicatorFus = indicatorFus;
  readonly etichetaTip = etichetaTipSedinta;
  readonly etichetaMod = etichetaModDesfasurare;
  readonly etichetaStatus = etichetaStatusSedinta;

  constructor() {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      this.sedinta.set(await this.api.detalii(this.id));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  editeaza(): void {
    this.router.navigate(['/sedinte', this.id, 'editeaza']);
  }

  async incepe(): Promise<void> {
    await this.tranziteaza(
      () => this.api.incepe(this.id),
      'Ședință începută.',
      {
        titlu: 'Începere ședință',
        mesaj: 'Marchezi ședința ca „În desfășurare"?',
        etichetaConfirmare: 'Începe'
      });
  }

  async finalizeaza(): Promise<void> {
    await this.tranziteaza(
      () => this.api.finalizeaza(this.id),
      'Ședință finalizată.',
      {
        titlu: 'Finalizare ședință',
        mesaj: 'Marchezi ședința ca „Finalizată"? Acțiunea este ireversibilă.',
        etichetaConfirmare: 'Finalizează',
        periculos: true
      });
  }

  async anuleaza(): Promise<void> {
    await this.tranziteaza(
      () => this.api.anuleaza(this.id),
      'Ședință anulată.',
      {
        titlu: 'Anulare ședință',
        mesaj: 'Marchezi ședința ca „Anulată"? Acțiunea este ireversibilă.',
        etichetaConfirmare: 'Anulează ședința',
        periculos: true
      });
  }

  async sterge(): Promise<void> {
    const s = this.sedinta()!;
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

    this.seExecuta.set(true);
    try {
      await this.api.sterge(this.id);
      this.snackBar.open('Ședință ștearsă.', 'Închide', { duration: 4000 });
      this.router.navigate(['/sedinte']);
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
      this.seExecuta.set(false);
    }
  }

  inapoiLaLista(): void {
    this.router.navigate(['/sedinte']);
  }

  private async tranziteaza(
    actiune: () => Promise<Sedinta>,
    mesajSucces: string,
    confirmare: DateConfirmare
  ): Promise<void> {
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog,
        { data: confirmare, width: '460px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    this.seExecuta.set(true);
    try {
      this.sedinta.set(await actiune());
      this.snackBar.open(mesajSucces, 'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.seExecuta.set(false);
    }
  }

  claseStatus(status: StatusSedinta): string {
    switch (status) {
      case StatusSedinta.Planificata: return 'badge badge-planificata';
      case StatusSedinta.Convocata: return 'badge badge-convocata';
      case StatusSedinta.InDesfasurare: return 'badge badge-indesfasurare';
      case StatusSedinta.Finalizata: return 'badge badge-finalizata';
      case StatusSedinta.Anulata: return 'badge badge-anulata';
    }
  }

  async reincarcaSedinta(): Promise<void> {
    try {
      this.sedinta.set(await this.api.detalii(this.id));
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }
  
}