import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/auth/auth.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { ConfirmareDialog, DateConfirmare } from '../../../shared/confirmare/confirmare-dialog';
import { formateazaDataDoar } from '../../../shared/data';
import { RaspunsPrefect, StatusActRedactional } from '../../../shared/enums';
import { etichetaCanalTransmiterePrefect, etichetaRaspunsPrefect } from '../../../shared/etichete';
import {
  ComunicareDialog, DateComunicareDialog
} from '../comunicare-dialog/comunicare-dialog';
import {
  DateRaspunsPrefectDialog, RaspunsPrefectDialog
} from '../raspuns-prefect-dialog/raspuns-prefect-dialog';
import { ComunicareHclPrefect } from '../hcl.models';
import { actiuniComunicari } from '../hcl.permisiuni';
import { HclService } from '../hcl.service';

const COLOANE = ['nrOrdine', 'dataTrimiteri', 'canal', 'raspuns'];

@Component({
  selector: 'app-comunicari-tab',
  imports: [
    MatTableModule, MatIconModule, MatButtonModule,
    MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './comunicari-tab.html',
  styleUrl: './comunicari-tab.scss'
})
export class ComunicariTab implements OnInit {
  private readonly api = inject(HclService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly hclId = input.required<number>();
  readonly status = input.required<StatusActRedactional>();

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly comunicari = signal<ComunicareHclPrefect[]>([]);
  readonly randuriBlocate = signal<ReadonlySet<number>>(new Set());

  readonly esteAdmin = computed(() => this.auth.areRol('Admin'));
  readonly esteAdminSauSecretar = computed(() => this.auth.areOricareRol('Admin', 'Secretar'));
  readonly actiuni = computed(() =>
    actiuniComunicari(this.status(), this.esteAdminSauSecretar(), this.esteAdmin()));

  readonly coloane = computed(() =>
    this.actiuni().poateEdita || this.actiuni().poateSterge ? [...COLOANE, 'actiuni'] : COLOANE);

  readonly StatusActRedactional = StatusActRedactional;
  readonly etichetaCanal = etichetaCanalTransmiterePrefect;
  readonly etichetaRaspuns = etichetaRaspunsPrefect;
  readonly formateazaDataDoar = formateazaDataDoar;

  ngOnInit(): void {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      this.comunicari.set(await this.api.comunicari(this.hclId()));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  async adauga(): Promise<void> {
    if (!this.actiuni().poateAdauga) {
      this.snackBar.open(
        'Comunicarea către prefect e posibilă doar după numerotarea hotărârii.',
        'Închide', { duration: 5000 });
      return;
    }
    const data: DateComunicareDialog = { hclId: this.hclId() };
    const rezultat = await firstValueFrom(
      this.dialog.open<ComunicareDialog, DateComunicareDialog, ComunicareHclPrefect | undefined>(
        ComunicareDialog, { data, width: '480px', maxWidth: '95vw' }).afterClosed());
    if (!rezultat) return;
    this.snackBar.open('Comunicarea a fost adăugată.', 'Închide', { duration: 3000 });
    await this.incarca();
  }

  async editeazaRaspuns(c: ComunicareHclPrefect): Promise<void> {
    if (!this.actiuni().poateEdita) return;
    const data: DateRaspunsPrefectDialog = { hclId: this.hclId(), comunicare: c };
    const rezultat = await firstValueFrom(
      this.dialog.open<RaspunsPrefectDialog, DateRaspunsPrefectDialog, ComunicareHclPrefect | undefined>(
        RaspunsPrefectDialog, { data, width: '520px', maxWidth: '95vw' }).afterClosed());
    if (!rezultat) return;
    // Nr. ordine imutabil → ordinea listei nu se schimbă; patch local în loc de refetch.
    this.comunicari.update(lista => lista.map(x => x.id === rezultat.id ? rezultat : x));
    this.snackBar.open('Răspunsul prefectului a fost salvat.', 'Închide', { duration: 3000 });
  }

  async sterge(c: ComunicareHclPrefect): Promise<void> {
    if (!this.actiuni().poateSterge || this.randuriBlocate().has(c.id)) return;
    const data: DateConfirmare = {
      titlu: 'Ștergere comunicare',
      mesaj: `Ștergi comunicarea nr. ${c.numarOrdineInRegistru}/${c.anRegistru} din registru? Acțiunea nu poate fi anulată.`,
      etichetaConfirmare: 'Șterge',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data, width: '480px', maxWidth: '95vw' }).afterClosed());
    if (!confirmat) return;

    this.blocheazaRand(c.id);
    try {
      await this.api.stergeComunicare(this.hclId(), c.id);
      this.snackBar.open('Comunicarea a fost ștearsă.', 'Închide', { duration: 3000 });
      await this.incarca();
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.deblocheazaRand(c.id);
    }
  }

  deschideRegistru(): void {
    this.router.navigate(['/hcl/registru-comunicari']);
  }

  esteRandBlocat(id: number): boolean {
    return this.randuriBlocate().has(id);
  }

  claseBadgeRaspuns(r: RaspunsPrefect | null): string {
    if (r == null) return 'badge badge-neutru';
    switch (r) {
      case RaspunsPrefect.Acceptat: return 'badge badge-acceptat';
      case RaspunsPrefect.RespinsLegalitate: return 'badge badge-respins';
      case RaspunsPrefect.CereClarificari: return 'badge badge-clarificari';
      case RaspunsPrefect.FaraRaspuns: return 'badge badge-neutru';
    }
  }

  private blocheazaRand(id: number): void {
    this.randuriBlocate.update(s => new Set(s).add(id));
  }

  private deblocheazaRand(id: number): void {
    this.randuriBlocate.update(s => {
      const nou = new Set(s);
      nou.delete(id);
      return nou;
    });
  }
}
