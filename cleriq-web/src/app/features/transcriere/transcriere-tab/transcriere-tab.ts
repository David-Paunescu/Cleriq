import {
  Component, ElementRef, OnDestroy, OnInit, computed, effect, inject, input,
  signal, viewChild
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom, Subscription } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/auth/auth.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { formateazaDataOra } from '../../../shared/data';
import { StatusTranscriere } from '../../../shared/enums';
import { etichetaStatusTranscriere } from '../../../shared/etichete';
import { ConfirmareDialog, DateConfirmare } from '../../../shared/confirmare/confirmare-dialog';
import {
  Transcriere, TranscriereContinut, formateazaDurataAudio, formateazaMarimeAudio,
  formateazaTimp, parseazaContinutBrut, valideazaAudio
} from '../transcriere.models';
import { actiuniPermise } from '../transcriere.permisiuni';
import { ProgresUpload, TranscriereService } from '../transcriere.service';

const INTERVAL_POLLING_MS = 5000;
const CHEIE_OPTIUNI = 'cleriq.transcriere.optiuni';

@Component({
  selector: 'app-transcriere-tab',
  imports: [
    MatCardModule, MatButtonModule, MatIconModule, MatMenuModule,
    MatProgressBarModule, MatProgressSpinnerModule, MatTooltipModule,
    MatCheckboxModule
  ],
  templateUrl: './transcriere-tab.html',
  styleUrl: './transcriere-tab.scss'
})
export class TranscriereTab implements OnInit, OnDestroy {
  private readonly api = inject(TranscriereService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly sedintaId = input.required<number>();
  readonly tabActiv = input(true);

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly transcriere = signal<Transcriere | null>(null);
  readonly dragInCurs = signal(false);
  readonly progresUpload = signal<ProgresUpload | null>(null);
  readonly eroareUpload = signal<string | null>(null);

  readonly continut = signal<TranscriereContinut | null>(null);
  readonly seIncarcaContinut = signal(false);
  readonly audioUrl = signal<string | null>(null);
  readonly seIncarcaAudio = signal(false);
  readonly eroareAudio = signal<string | null>(null);
  readonly afiseazaSpeaker = signal(true);
  readonly afiseazaTimestamp = signal(false);

  readonly playerAudio = viewChild<ElementRef<HTMLAudioElement>>('playerAudio');

  private subscriereUpload: Subscription | null = null;
  private intervalId: ReturnType<typeof setInterval> | null = null;
  private vizibilitateHandler: (() => void) | null = null;

  readonly esteAdmin = computed(() => this.auth.areRol('Admin'));
  readonly esteAdminSauSecretar = computed(() => this.auth.areOricareRol('Admin', 'Secretar'));

  readonly actiuni = computed(() =>
    actiuniPermise(this.transcriere()?.status ?? null, this.esteAdmin(), this.esteAdminSauSecretar()));

  readonly esteInLucru = computed(() => {
    const s = this.transcriere()?.status;
    return s === StatusTranscriere.InAsteptare || s === StatusTranscriere.InProces;
  });

  readonly segmente = computed(() =>
    parseazaContinutBrut(this.continut()?.continutBrut ?? null));

  readonly continutPentruConsilier = computed(() => {
    const c = this.continut();
    if (!c || c.status !== StatusTranscriere.Finalizata) return null;
    return c.continutEditat;
  });

  readonly StatusTranscriere = StatusTranscriere;
  readonly etichetaStatusTranscriere = etichetaStatusTranscriere;
  readonly formateazaDataOra = formateazaDataOra;
  readonly formateazaMarimeAudio = formateazaMarimeAudio;
  readonly formateazaDurataAudio = formateazaDurataAudio;
  readonly formateazaTimp = formateazaTimp;

  constructor() {
    const optiuni = this.citesteOptiuni();
    this.afiseazaSpeaker.set(optiuni.speaker);
    this.afiseazaTimestamp.set(optiuni.timestamp);

    effect(() => {
      if (this.tabActiv() && this.esteInLucru() && this.progresUpload() === null) {
        this.porneestePolling();
      } else {
        this.opresteePolling();
      }
    });

    effect(() => {
      const t = this.transcriere();
      if (t?.status === StatusTranscriere.Finalizata) {
        this.asigurareIncarcatContinut();
        if (this.actiuni().poateDescarcaAudio) {
          this.asigurareIncarcatAudio();
        }
      }
    });

    effect(() => {
      this.afiseazaSpeaker();
      this.afiseazaTimestamp();
      this.salveazaOptiuni();
    });
  }

  ngOnInit(): void {
    this.incarca();
    this.vizibilitateHandler = () => {
      if (document.visibilityState === 'visible'
          && this.tabActiv()
          && this.esteInLucru()) {
        this.incarcaFaraSpinner();
      }
    };
    document.addEventListener('visibilitychange', this.vizibilitateHandler);
  }

  ngOnDestroy(): void {
    this.opresteePolling();
    this.subscriereUpload?.unsubscribe();
    this.curataAudio();
    if (this.vizibilitateHandler) {
      document.removeEventListener('visibilitychange', this.vizibilitateHandler);
      this.vizibilitateHandler = null;
    }
  }

  private porneestePolling(): void {
    if (this.intervalId !== null) return;
    this.intervalId = setInterval(() => this.incarcaFaraSpinner(), INTERVAL_POLLING_MS);
  }

  private opresteePolling(): void {
    if (this.intervalId !== null) {
      clearInterval(this.intervalId);
      this.intervalId = null;
    }
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      this.transcriere.set(await this.api.detalii(this.sedintaId()));
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 404) {
        this.transcriere.set(null);
      } else {
        this.eroare.set(extrageMesajEroare(err));
      }
    } finally {
      this.seIncarca.set(false);
    }
  }

  private async incarcaFaraSpinner(): Promise<void> {
    try {
      this.transcriere.set(await this.api.detalii(this.sedintaId()));
    } catch {
      // ignorat la polling
    }
  }

  private async asigurareIncarcatContinut(): Promise<void> {
    if (this.continut() || this.seIncarcaContinut()) return;
    this.seIncarcaContinut.set(true);
    try {
      this.continut.set(await this.api.obtineContinut(this.sedintaId()));
    } catch {
      this.snackBar.open('Nu s-a putut încărca conținutul transcrierii.',
        'Închide', { duration: 5000 });
    } finally {
      this.seIncarcaContinut.set(false);
    }
  }

  private async asigurareIncarcatAudio(): Promise<void> {
    if (this.audioUrl() || this.seIncarcaAudio()) return;
    this.seIncarcaAudio.set(true);
    this.eroareAudio.set(null);
    try {
      const blob = await this.api.obtineBlobAudio(this.sedintaId());
      this.audioUrl.set(URL.createObjectURL(blob));
    } catch {
      this.eroareAudio.set('Audio nu a putut fi încărcat pentru playback.');
    } finally {
      this.seIncarcaAudio.set(false);
    }
  }

  private curataAudio(): void {
    const url = this.audioUrl();
    if (url) {
      URL.revokeObjectURL(url);
      this.audioUrl.set(null);
    }
    this.continut.set(null);
    this.eroareAudio.set(null);
  }

  seek(secunde: number): void {
    if (!this.audioUrl()) return;
    const audio = this.playerAudio()?.nativeElement;
    if (audio) {
      audio.currentTime = secunde;
      audio.play().catch(() => { /* autoplay restrictions, ignorăm */ });
    }
  }

  comutaSpeaker(): void {
    this.afiseazaSpeaker.update(v => !v);
  }

  comutaTimestamp(): void {
    this.afiseazaTimestamp.update(v => !v);
  }

  private citesteOptiuni(): { speaker: boolean; timestamp: boolean } {
    try {
      const raw = localStorage.getItem(CHEIE_OPTIUNI);
      if (!raw) return { speaker: true, timestamp: false };
      const parsed = JSON.parse(raw) as Record<string, unknown>;
      return {
        speaker: typeof parsed['speaker'] === 'boolean' ? parsed['speaker'] : true,
        timestamp: typeof parsed['timestamp'] === 'boolean' ? parsed['timestamp'] : false
      };
    } catch {
      return { speaker: true, timestamp: false };
    }
  }

  private salveazaOptiuni(): void {
    try {
      localStorage.setItem(CHEIE_OPTIUNI, JSON.stringify({
        speaker: this.afiseazaSpeaker(),
        timestamp: this.afiseazaTimestamp()
      }));
    } catch {
      // quota exceeded / private browsing — ignorat
    }
  }

  laDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (this.actiuni().poateIncarca && !this.progresUpload()) {
      this.dragInCurs.set(true);
    }
  }

  laDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragInCurs.set(false);
  }

  laDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragInCurs.set(false);

    if (!this.actiuni().poateIncarca || this.progresUpload()) return;

    const fisier = event.dataTransfer?.files?.[0];
    if (fisier) this.porneesteUpload(fisier);
  }

  laFisierSelectat(event: Event): void {
    const input = event.target as HTMLInputElement;
    const fisier = input.files?.[0];
    if (fisier) this.porneesteUpload(fisier);
    input.value = '';
  }

  private porneesteUpload(fisier: File): void {
    const eroareValidare = valideazaAudio(fisier.name, fisier.size);
    if (eroareValidare) {
      this.eroareUpload.set(eroareValidare);
      this.snackBar.open(eroareValidare, 'Închide', { duration: 5000 });
      return;
    }

    this.eroareUpload.set(null);
    this.progresUpload.set({
      procentaj: 0,
      octetiTotali: fisier.size,
      octetiIncarcati: 0,
      rezultat: null
    });

    this.subscriereUpload = this.api.incarca(this.sedintaId(), fisier).subscribe({
      next: (progres) => {
        this.progresUpload.set(progres);
        if (progres.rezultat) {
          this.curataAudio();
          this.transcriere.set(progres.rezultat);
          this.progresUpload.set(null);
          this.snackBar.open('Audio încărcat. Transcrierea va începe în curând.',
            'Închide', { duration: 5000 });
        }
      },
      error: (err) => {
        this.progresUpload.set(null);
        const mesaj = extrageMesajEroare(err);
        this.eroareUpload.set(mesaj);
        this.snackBar.open(mesaj, 'Închide', { duration: 5000 });
      }
    });
  }

  anuleazaUpload(): void {
    this.subscriereUpload?.unsubscribe();
    this.subscriereUpload = null;
    this.progresUpload.set(null);
    this.snackBar.open('Upload anulat.', 'Închide', { duration: 3000 });
  }

  async retry(): Promise<void> {
    const date: DateConfirmare = {
      titlu: 'Reîncearcă transcrierea',
      mesaj: 'Trimitem audio-ul existent înapoi la procesare. Audio-ul nu se re-încarcă.',
      etichetaConfirmare: 'Reîncearcă'
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '440px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    try {
      const rezultat = await this.api.retry(this.sedintaId());
      this.transcriere.set(rezultat);
      this.snackBar.open('Transcrierea a fost reprogramată.', 'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }

  async sterge(): Promise<void> {
    const date: DateConfirmare = {
      titlu: 'Ștergere transcriere',
      mesaj: 'Ștergi transcrierea curentă (inclusiv conținutul brut și editat)? După ștergere, poți încărca audio nou pentru a începe o nouă transcriere.',
      etichetaConfirmare: 'Șterge',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '520px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    try {
      await this.api.sterge(this.sedintaId());
      this.curataAudio();
      this.transcriere.set(null);
      this.snackBar.open('Transcriere ștearsă.', 'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }

  async descarcaAudio(): Promise<void> {
    try {
      await this.api.descarcaAudio(this.sedintaId(),
        `transcriere-sedinta-${this.sedintaId()}`);
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }

  iconaStatus(status: StatusTranscriere): string {
    switch (status) {
      case StatusTranscriere.InAsteptare: return 'hourglass_empty';
      case StatusTranscriere.InProces: return 'sync';
      case StatusTranscriere.Esuata: return 'error_outline';
      case StatusTranscriere.Finalizata: return 'check_circle';
    }
  }

  mesajStatus(t: Transcriere): string {
    switch (t.status) {
      case StatusTranscriere.InAsteptare:
        return 'Audio-ul așteaptă să fie preluat pentru procesare.';
      case StatusTranscriere.InProces:
        return 'Whisper procesează audio-ul. Aceasta poate dura câteva minute, în funcție de durata înregistrării.';
      case StatusTranscriere.Esuata:
        return 'Transcrierea a eșuat după mai multe încercări. Poți reîncerca sau înlocui audio-ul.';
      case StatusTranscriere.Finalizata:
        return 'Transcrierea este gata. Verifică conținutul mai jos.';
    }
  }
}