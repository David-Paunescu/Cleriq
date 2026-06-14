import { Component, OnDestroy, OnInit, computed, effect, inject, input, output, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/auth/auth.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { ConfirmareDialog, DateConfirmare } from '../../../shared/confirmare/confirmare-dialog';
import { formateazaDataOra } from '../../../shared/data';
import { StatusConvocare, StatusSedinta, StatusTrimitere } from '../../../shared/enums';
import {
  etichetaStatusConvocare, etichetaStatusSedinta, etichetaStatusTrimitere
} from '../../../shared/etichete';
import { normalizeazaPentruCautare } from '../../../shared/text';
import {
  DateIncercariDialog, IncercariDialog
} from '../incercari-dialog/incercari-dialog';
import {
  Convocare, areCanalEsuat, existaInAsteptare, numaratori
} from '../convocari.models';
import { actiuniPermise } from '../convocari.permisiuni';
import { ConvocariService } from '../convocari.service';

const COLOANE_BAZA = ['consilier', 'email', 'sms', 'statusGeneral'];
const INTERVAL_POLLING_MS = 3000;

@Component({
  selector: 'app-convocari-tab',
  imports: [
    MatTableModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatIconModule, MatButtonModule, MatMenuModule,
    MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './convocari-tab.html',
  styleUrl: './convocari-tab.scss'
})
export class ConvocariTab implements OnInit, OnDestroy {
  private readonly api = inject(ConvocariService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly sedintaId = input.required<number>();
  readonly statusSedinta = input.required<StatusSedinta>();
  readonly tabActiv = input(true);
  readonly sedintaSchimbata = output<void>();

  readonly seIncarca = signal(false);
  readonly seTrimite = signal(false);
  readonly seReseteaza = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly convocari = signal<Convocare[]>([]);
  readonly randuriBlocate = signal<ReadonlySet<number>>(new Set());
  readonly filtru = signal('');
  readonly filtruStatus = signal<StatusConvocare | 'toate'>('toate');
  readonly pollingActiv = signal(false);

  private intervalId: ReturnType<typeof setInterval> | null = null;
  private vizibilitateHandler: (() => void) | null = null;

  readonly actiuni = computed(() =>
    actiuniPermise(this.statusSedinta(), this.auth.areOricareRol('Admin', 'Secretar')));

  readonly numaratoriCanale = computed(() => numaratori(this.convocari()));

  readonly convocariFiltrate = computed(() => {
    const termen = normalizeazaPentruCautare(this.filtru());
    const status = this.filtruStatus();
    return this.convocari()
      .filter(c => status === 'toate' || c.statusGeneral === status)
      .filter(c => !termen
        || normalizeazaPentruCautare(c.numeCompletConsilier).includes(termen));
  });

  readonly coloane = computed(() =>
    !this.actiuni().esteReadOnly ? [...COLOANE_BAZA, 'actiuni'] : COLOANE_BAZA);

  readonly statusuriConvocare = [
    StatusConvocare.TotalSucces, StatusConvocare.PartialSucces,
    StatusConvocare.Esuata, StatusConvocare.FaraCoordonate,
    StatusConvocare.InCursDeTrimitere
  ];

  readonly StatusTrimitere = StatusTrimitere;
  readonly etichetaStatusTrimitere = etichetaStatusTrimitere;
  readonly etichetaStatusConvocare = etichetaStatusConvocare;
  readonly etichetaStatusSedinta = etichetaStatusSedinta;
  readonly formateazaDataOra = formateazaDataOra;

  constructor() {
    effect(() => {
      const lista = this.convocari();
      const peTab = this.tabActiv();
      if (peTab && existaInAsteptare(lista) && !this.actiuni().esteReadOnly) {
        this.porneestePolling();
      } else {
        this.opresteePolling();
      }
    });
  }

  ngOnInit(): void {
    this.incarca();
    this.vizibilitateHandler = () => {
      if (document.visibilityState === 'visible'
          && this.tabActiv()
          && existaInAsteptare(this.convocari())) {
        this.incarca();
      }
    };
    document.addEventListener('visibilitychange', this.vizibilitateHandler);
  }

  ngOnDestroy(): void {
    this.opresteePolling();
    if (this.vizibilitateHandler) {
      document.removeEventListener('visibilitychange', this.vizibilitateHandler);
      this.vizibilitateHandler = null;
    }
  }

  private porneestePolling(): void {
    if (this.intervalId !== null) return;
    this.pollingActiv.set(true);
    this.intervalId = setInterval(() => this.incarcaFaraSpinner(), INTERVAL_POLLING_MS);
  }

  private opresteePolling(): void {
    if (this.intervalId !== null) {
      clearInterval(this.intervalId);
      this.intervalId = null;
    }
    this.pollingActiv.set(false);
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      const lista = await this.api.lista(this.sedintaId());
      this.convocari.set([...lista].sort((a, b) =>
        a.numeCompletConsilier.localeCompare(b.numeCompletConsilier, 'ro')));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  // Polling silentios: fără spinner global, fără să șteargă eroare existentă din UI
  private async incarcaFaraSpinner(): Promise<void> {
    try {
      const lista = await this.api.lista(this.sedintaId());
      this.convocari.set([...lista].sort((a, b) =>
        a.numeCompletConsilier.localeCompare(b.numeCompletConsilier, 'ro')));
    } catch {
      // Erorile transiente la polling sunt ignorate silent — următoarea tură va reuși.
      // Eroarea persistentă apare la refresh manual via incarca().
    }
  }

  laCautare(event: Event): void {
    this.filtru.set((event.target as HTMLInputElement).value);
  }

  laFiltruStatus(valoare: StatusConvocare | 'toate'): void {
    this.filtruStatus.set(valoare);
  }

  async trimite(): Promise<void> {
    const date: DateConfirmare = {
      titlu: 'Trimite convocarea',
      mesaj: 'Toți consilierii activi vor primi imediat email și SMS de convocare, conform datelor lor de contact. Conținutul mesajelor este înghețat la trimitere și nu mai poate fi modificat.',
      etichetaConfirmare: 'Trimite'
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '520px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    this.seTrimite.set(true);
    try {
      const rezultat = await this.api.trimite(this.sedintaId());
      this.snackBar.open(
        `Convocările au fost create pentru ${rezultat.totalConsilieri} consilieri. Trimiterea se procesează în fundal.`,
        'Închide', { duration: 6000 });
      await this.incarca();
      this.sedintaSchimbata.emit();
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.seTrimite.set(false);
    }
  }

  async retry(convocare: Convocare): Promise<void> {
    if (this.randuriBlocate().has(convocare.id)) return;
    this.blocheazaRand(convocare.id);
    try {
      const rezultat = await this.api.retry(this.sedintaId(), convocare.id);
      this.actualizeazaLocal(rezultat);
      this.snackBar.open(
        `Convocarea către ${convocare.numeCompletConsilier} a fost reprogramată.`,
        'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.deblocheazaRand(convocare.id);
    }
  }

  vezi(convocare: Convocare): void {
    const data: DateIncercariDialog = {
      sedintaId: this.sedintaId(),
      convocareId: convocare.id,
      numeCompletConsilier: convocare.numeCompletConsilier
    };
    this.dialog.open(IncercariDialog, {
      data, width: '900px', maxWidth: '95vw'
    });
  }

  async reseteaza(): Promise<void> {
    const date: DateConfirmare = {
      titlu: 'Resetare completă convocări',
      mesaj: 'Acțiunea șterge TOATE convocările și istoricul lor de încercări. Ședința revine la status Planificată. Folosește această opțiune doar dacă vrei să recreezi convocările de la zero.',
      etichetaConfirmare: 'Resetează',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '520px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    this.seReseteaza.set(true);
    try {
      await this.api.reseteaza(this.sedintaId());
      this.snackBar.open('Convocările au fost resetate.', 'Închide', { duration: 4000 });
      this.convocari.set([]);
      this.sedintaSchimbata.emit();
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.seReseteaza.set(false);
    }
  }

  esteRandBlocat(id: number): boolean {
    return this.randuriBlocate().has(id);
  }

  areCanalEsuat(convocare: Convocare): boolean {
    return areCanalEsuat(convocare);
  }

  claseBadgeTrimitere(s: StatusTrimitere | null): string {
    if (s == null) return 'badge badge-neutru';
    switch (s) {
      case StatusTrimitere.Trimisa: return 'badge badge-trimisa';
      case StatusTrimitere.Esuata: return 'badge badge-esuata';
      case StatusTrimitere.InAsteptare: return 'badge badge-asteptare';
      case StatusTrimitere.FaraDestinatie: return 'badge badge-neutru';
    }
  }

  claseBadgeConvocare(s: StatusConvocare): string {
    switch (s) {
      case StatusConvocare.TotalSucces: return 'badge badge-succes';
      case StatusConvocare.PartialSucces: return 'badge badge-partial';
      case StatusConvocare.Esuata: return 'badge badge-esuata';
      case StatusConvocare.FaraCoordonate: return 'badge badge-neutru';
      case StatusConvocare.InCursDeTrimitere: return 'badge badge-asteptare';
    }
  }

  private actualizeazaLocal(rezultat: Convocare): void {
    this.convocari.update(lista => lista.map(c =>
      c.id === rezultat.id ? rezultat : c));
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