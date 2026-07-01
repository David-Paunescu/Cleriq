import { Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
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
  ComunicareDispozitieDialog, DateComunicareDispozitieDialog
} from '../comunicare-dialog/comunicare-dialog';
import {
  DateRaspunsPrefectDispozitieDialog, RaspunsPrefectDispozitieDialog
} from '../raspuns-prefect-dialog/raspuns-prefect-dialog';
import { ComunicareDispozitiePrefect } from '../dispozitii.models';
import { actiuniComunicari } from '../dispozitii.permisiuni';
import { DispozitiiService } from '../dispozitii.service';

const COLOANE = ['nrOrdine', 'dataTrimiteri', 'canal', 'raspuns'];

@Component({
  selector: 'app-comunicari-dispozitie-tab',
  imports: [
    MatTableModule, MatIconModule, MatButtonModule,
    MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './comunicari-tab.html',
  styleUrl: './comunicari-tab.scss'
})
export class ComunicariDispozitieTab implements OnInit {
  private readonly api = inject(DispozitiiService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly dispozitieId = input.required<number>();
  readonly status = input.required<StatusActRedactional>();

  // ◆ CRITIC (staleness) — spre deosebire de comunicari-tab din HCL (care n-are output): pe add/delete
  // se schimbă pe backend latch-ul AIntratInCircuit (prima comunicare) și areComunicari (ultima ștearsă)
  // → hub-ul trebuie să refacă incarca(), altfel antetul + semnatarii rămân stale și butoanele de
  // variantă semnată / „Anulează MOL" declanșează 409. Editarea răspunsului NU emite (nu schimbă latch-ul).
  readonly schimbat = output<void>();

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly comunicari = signal<ComunicareDispozitiePrefect[]>([]);
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
      this.comunicari.set(await this.api.comunicari(this.dispozitieId()));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  async adauga(): Promise<void> {
    if (!this.actiuni().poateAdauga) {
      this.snackBar.open(
        'Comunicarea către prefect e posibilă doar după numerotarea dispoziției.',
        'Închide', { duration: 5000 });
      return;
    }
    const data: DateComunicareDispozitieDialog = { dispozitieId: this.dispozitieId() };
    const rezultat = await firstValueFrom(
      this.dialog.open<ComunicareDispozitieDialog, DateComunicareDispozitieDialog, ComunicareDispozitiePrefect | undefined>(
        ComunicareDispozitieDialog, { data, width: '480px', maxWidth: '95vw' }).afterClosed());
    if (!rezultat) return;
    this.snackBar.open('Comunicarea a fost adăugată.', 'Închide', { duration: 3000 });
    await this.incarca();
    this.schimbat.emit();  // prima comunicare aprinde latch-ul → hub refetch (antet + semnatari)
  }

  async editeazaRaspuns(c: ComunicareDispozitiePrefect): Promise<void> {
    if (!this.actiuni().poateEdita) return;
    const data: DateRaspunsPrefectDispozitieDialog = { dispozitieId: this.dispozitieId(), comunicare: c };
    const rezultat = await firstValueFrom(
      this.dialog.open<RaspunsPrefectDispozitieDialog, DateRaspunsPrefectDispozitieDialog, ComunicareDispozitiePrefect | undefined>(
        RaspunsPrefectDispozitieDialog, { data, width: '520px', maxWidth: '95vw' }).afterClosed());
    if (!rezultat) return;
    // Nr. ordine imutabil → ordinea listei nu se schimbă; patch local în loc de refetch. Nu emite
    // `schimbat`: răspunsul nu atinge latch-ul / numărul de comunicări, deci hub-ul rămâne valid.
    this.comunicari.update(lista => lista.map(x => x.id === rezultat.id ? rezultat : x));
    this.snackBar.open('Răspunsul prefectului a fost salvat.', 'Închide', { duration: 3000 });
  }

  async sterge(c: ComunicareDispozitiePrefect): Promise<void> {
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
      await this.api.stergeComunicare(this.dispozitieId(), c.id);
      this.snackBar.open('Comunicarea a fost ștearsă.', 'Închide', { duration: 3000 });
      await this.incarca();
      this.schimbat.emit();  // ultima comunicare ștearsă → areComunicari=false → „Anulează MOL" redevine posibil
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.deblocheazaRand(c.id);
    }
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
