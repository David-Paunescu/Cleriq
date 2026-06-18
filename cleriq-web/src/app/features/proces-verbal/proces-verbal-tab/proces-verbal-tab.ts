import {
  Component, ElementRef, OnDestroy, OnInit, computed, effect, inject, input,
  signal, viewChild
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/auth/auth.service';
import {
  ModificariNesalvateService, ProprietarStareModificari
} from '../../../core/modificari/modificari-nesalvate.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { ConfirmareDialog, DateConfirmare } from '../../../shared/confirmare/confirmare-dialog';
import { formateazaDataOra } from '../../../shared/data';
import { StatusProcesVerbal } from '../../../shared/enums';
import { etichetaStatusProcesVerbal } from '../../../shared/etichete';
import { ProcesVerbal, formateazaMarimePv, valideazaPdfSemnat } from '../proces-verbal.models';
import { actiuniPermise } from '../proces-verbal.permisiuni';
import { ProcesVerbalService } from '../proces-verbal.service';

const DEBOUNCE_AUTOSAVE_MS = 2000;

@Component({
  selector: 'app-proces-verbal-tab',
  imports: [
    MatCardModule, MatButtonModule, MatIconModule, MatMenuModule,
    MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './proces-verbal-tab.html',
  styleUrl: './proces-verbal-tab.scss'
})
export class ProcesVerbalTab implements OnInit, OnDestroy {
  private readonly api = inject(ProcesVerbalService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly modificari = inject(ModificariNesalvateService);

  readonly sedintaId = input.required<number>();
  readonly tabActiv = input(true);

  readonly seIncarca = signal(false);
  readonly seGenereaza = signal(false);
  readonly seDescarcaPdf = signal(false);
  readonly seIncarcaSemnat = signal(false);
  readonly seDescarcaSemnat = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly procesVerbal = signal<ProcesVerbal | null>(null);

  readonly valoareEditor = signal('');
  readonly ultimaValoareSalvata = signal('');
  readonly valoareEditorInitializata = signal(false);
  readonly dataUltimeiSalvari = signal<Date | null>(null);
  readonly seSalveaza = signal(false);
  readonly eroareSalvare = signal<string | null>(null);
  readonly dirtyPersistent = signal(false);
  readonly actiuneInCurs = signal(false);

  readonly textareaEditor = viewChild<ElementRef<HTMLTextAreaElement>>('textareaEditor');
  readonly inputFisierSemnat = viewChild<ElementRef<HTMLInputElement>>('inputFisierSemnat');

  private vizibilitateHandler: (() => void) | null = null;
  private proprietar!: ProprietarStareModificari;

  readonly esteAdmin = computed(() => this.auth.areRol('Admin'));
  readonly esteAdminSauSecretar = computed(() => this.auth.areOricareRol('Admin', 'Secretar'));

  readonly areSemnat = computed(() =>
    this.procesVerbal()?.numeFisierSemnat != null);

  readonly actiuni = computed(() =>
    actiuniPermise(
      this.procesVerbal()?.status ?? null,
      this.areSemnat(),
      this.esteAdmin(),
      this.esteAdminSauSecretar()));

  readonly esteDownloadGeneratPrimar = computed(() =>
    this.procesVerbal()?.status === StatusProcesVerbal.Finalizat
    && !this.areSemnat());

  readonly esteDirty = computed(() =>
    this.valoareEditor() !== this.ultimaValoareSalvata());

  readonly afiseazaIndicator = computed(() => {
    if (this.eroareSalvare()) {
      return { clasa: 'stare-eroare', icon: 'error_outline', spinner: false, text: 'Eroare la salvare. Reîncearcă.' };
    }
    if (this.dirtyPersistent() && !this.seSalveaza()) {
      return { clasa: 'stare-dirty', icon: 'warning', spinner: false, text: 'Modificări nesalvate' };
    }
    const data = this.dataUltimeiSalvari();
    if (data) {
      const ora = data.toLocaleTimeString('ro-RO', { hour: '2-digit', minute: '2-digit' });
      return {
        clasa: 'stare-salvat',
        icon: this.seSalveaza() ? null : 'check_circle',
        spinner: this.seSalveaza(),
        text: `Salvat la ${ora}`
      };
    }
    if (this.seSalveaza()) {
      return { clasa: 'stare-salvat', icon: null, spinner: true, text: 'Se salvează...' };
    }
    return null;
  });

  readonly StatusProcesVerbal = StatusProcesVerbal;
  readonly etichetaStatusProcesVerbal = etichetaStatusProcesVerbal;
  readonly formateazaDataOra = formateazaDataOra;
  readonly formateazaMarimePv = formateazaMarimePv;

  private readonly handlerKeydown = (event: KeyboardEvent): void => {
    if (!this.tabActiv()) return;
    if (!(event.ctrlKey || event.metaKey)) return;
    if (event.key.toLowerCase() !== 's') return;
    event.preventDefault();
    if (this.actiuni().poateEdita && !this.actiuneInCurs()) {
      this.salveazaImediat(false);
    }
  };

  constructor() {
    effect(() => {
      const pv = this.procesVerbal();
      if (pv && !this.valoareEditorInitializata()) {
        const valoare = pv.continut ?? '';
        this.valoareEditor.set(valoare);
        this.ultimaValoareSalvata.set(valoare);
        this.valoareEditorInitializata.set(true);
      }
    });

    effect(() => {
      const valoare = this.valoareEditor();
      const ref = this.textareaEditor();
      if (ref && ref.nativeElement.value !== valoare) {
        ref.nativeElement.value = valoare;
      }
    });

    effect((onCleanup) => {
      if (this.actiuneInCurs()) return;
      if (!this.esteDirty() || this.seSalveaza()) {
        this.dirtyPersistent.set(false);
        return;
      }
      const timerSave = setTimeout(() => this.salveazaImediat(true), DEBOUNCE_AUTOSAVE_MS);
      const timerDirty = setTimeout(() => this.dirtyPersistent.set(true), 10_000);
      onCleanup(() => {
        clearTimeout(timerSave);
        clearTimeout(timerDirty);
      });
    });
  }

  ngOnInit(): void {
    this.proprietar = {
      id: `proces-verbal-tab-${this.sedintaId()}`,
      areModificariNesalvate: () => this.esteDirty()
    };
    this.modificari.inregistreaza(this.proprietar);

    this.incarca();

    this.vizibilitateHandler = () => {
      if (document.visibilityState === 'hidden'
          && this.esteDirty()
          && !this.seSalveaza()
          && !this.actiuneInCurs()) {
        this.salveazaImediat(true);
      }
    };
    document.addEventListener('visibilitychange', this.vizibilitateHandler);
    window.addEventListener('keydown', this.handlerKeydown);
  }

  ngOnDestroy(): void {
    if (this.vizibilitateHandler) {
      document.removeEventListener('visibilitychange', this.vizibilitateHandler);
      this.vizibilitateHandler = null;
    }
    window.removeEventListener('keydown', this.handlerKeydown);
    this.modificari.retragere(this.proprietar.id);
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      this.procesVerbal.set(await this.api.obtine(this.sedintaId()));
    } catch (err) {
      if (err instanceof HttpErrorResponse && err.status === 404) {
        this.procesVerbal.set(null);
      } else {
        this.eroare.set(extrageMesajEroare(err));
      }
    } finally {
      this.seIncarca.set(false);
    }
  }

  async genereaza(): Promise<void> {
    if (!this.actiuni().poateGenera) return;

    this.seGenereaza.set(true);
    try {
      const rezultat = await this.api.genereaza(this.sedintaId());
      this.procesVerbal.set(rezultat);
      this.snackBar.open('Procesul verbal a fost generat.', 'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.seGenereaza.set(false);
    }
  }

  laInputEditor(event: Event): void {
    this.valoareEditor.set((event.target as HTMLTextAreaElement).value);
  }

  private async salveazaImediat(silent: boolean): Promise<void> {
    if (!this.esteDirty() || this.seSalveaza()) return;

    const valoare = this.valoareEditor();
    const inceput = Date.now();
    this.seSalveaza.set(true);
    this.eroareSalvare.set(null);

    try {
      const rezultat = await this.api.editeaza(this.sedintaId(),
        { continut: valoare });

      this.procesVerbal.set(rezultat);
      this.ultimaValoareSalvata.set(valoare);

      if (!this.esteDirty()) {
        const ramas = 600 - (Date.now() - inceput);
        if (ramas > 0) await new Promise(r => setTimeout(r, ramas));
      }

      this.dataUltimeiSalvari.set(new Date());

      if (!silent) {
        this.snackBar.open('Procesul verbal a fost salvat.', 'Închide', { duration: 3000 });
      }
    } catch (err) {
      const mesaj = extrageMesajEroare(err);
      this.eroareSalvare.set(mesaj);
      if (!silent) {
        this.snackBar.open(mesaj, 'Închide', { duration: 5000 });
      }
    } finally {
      this.seSalveaza.set(false);
    }
  }

  async finalizeaza(): Promise<void> {
    if (!this.actiuni().poateFinaliza || this.actiuneInCurs()) return;

    const date: DateConfirmare = {
      titlu: 'Finalizare proces verbal',
      mesaj: 'După finalizare, procesul verbal devine ireversibil: nu mai poate fi editat și nu mai poate fi regenerat. Va fi vizibil pe portalul public conform Legii 52/2003.',
      etichetaConfirmare: 'Finalizează',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '520px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    this.actiuneInCurs.set(true);
    try {
      if (this.esteDirty()) {
        await this.salveazaImediat(true);
        if (this.eroareSalvare() || this.esteDirty()) {
          this.snackBar.open(
            'Salvarea modificărilor a eșuat. Finalizarea a fost anulată.',
            'Închide', { duration: 5000 });
          return;
        }
      }

      const rezultat = await this.api.finalizeaza(this.sedintaId());
      this.procesVerbal.set(rezultat);
      this.snackBar.open('Procesul verbal a fost finalizat.', 'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.actiuneInCurs.set(false);
    }
  }

  async regenereaza(): Promise<void> {
    if (!this.actiuni().poateGenera || this.actiuneInCurs()) return;

    if (this.esteDirty()) {
      this.snackBar.open(
        'Așteaptă salvarea automată sau apasă Ctrl+S înainte de regenerare.',
        'Închide', { duration: 5000 });
      return;
    }

    const date: DateConfirmare = {
      titlu: 'Regenerare din date',
      mesaj: 'Toate editările manuale ale conținutului vor fi pierdute definitiv. Conținutul va fi regenerat din ordinea de zi, voturi și prezența curente.',
      etichetaConfirmare: 'Regenerează',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '520px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    this.actiuneInCurs.set(true);
    try {
      const rezultat = await this.api.genereaza(this.sedintaId());
      const valoareNoua = rezultat.continut ?? '';
      this.valoareEditor.set(valoareNoua);
      this.ultimaValoareSalvata.set(valoareNoua);
      this.procesVerbal.set(rezultat);
      this.dataUltimeiSalvari.set(null);
      this.eroareSalvare.set(null);
      this.snackBar.open('Procesul verbal a fost regenerat.', 'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.actiuneInCurs.set(false);
    }
  }

  async descarcaPdf(): Promise<void> {
    if (!this.actiuni().poateDescarcaPdf || this.seDescarcaPdf()) return;

    this.seDescarcaPdf.set(true);
    try {
      const data = new Date().toISOString().slice(0, 10);
      await this.api.descarcaPdf(this.sedintaId(), `proces-verbal-${data}.pdf`);
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.seDescarcaPdf.set(false);
    }
  }

  incepIncarcareSemnat(): void {
    if (!this.actiuni().poateIncarcaSemnat || this.seIncarcaSemnat()) return;
    this.inputFisierSemnat()?.nativeElement.click();
  }

  async incepInlocuireSemnat(): Promise<void> {
    if (!this.actiuni().poateIncarcaSemnat || this.seIncarcaSemnat()) return;

    const date: DateConfirmare = {
      titlu: 'Înlocuire variantă semnată',
      mesaj: 'Varianta semnată curentă va fi înlocuită cu noul fișier. Portalul public va afișa noua variantă imediat.',
      etichetaConfirmare: 'Continuă'
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '480px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    this.inputFisierSemnat()?.nativeElement.click();
  }

  async laFisierSemnatSelectat(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const fisier = input.files?.[0];
    input.value = '';

    if (!fisier) return;

    const eroareValidare = valideazaPdfSemnat(fisier.name, fisier.size);
    if (eroareValidare) {
      this.snackBar.open(eroareValidare, 'Închide', { duration: 5000 });
      return;
    }

    this.seIncarcaSemnat.set(true);
    try {
      const rezultat = await this.api.incarcaSemnat(this.sedintaId(), fisier);
      this.procesVerbal.set(rezultat);
      this.snackBar.open('Varianta semnată a fost încărcată.', 'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.seIncarcaSemnat.set(false);
    }
  }

  async descarcaSemnat(): Promise<void> {
    if (!this.actiuni().poateDescarcaSemnat || this.seDescarcaSemnat()) return;

    this.seDescarcaSemnat.set(true);
    try {
      const data = new Date().toISOString().slice(0, 10);
      await this.api.descarcaSemnat(this.sedintaId(), `proces-verbal-${data}-semnat.pdf`);
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.seDescarcaSemnat.set(false);
    }
  }

  async stergeSemnat(): Promise<void> {
    if (!this.actiuni().poateStergeSemnat) return;

    const date: DateConfirmare = {
      titlu: 'Ștergere variantă semnată',
      mesaj: 'Varianta semnată va fi eliminată definitiv din această ședință. Portalul public va reveni la PDF-ul generat (nesemnat) până la încărcarea unei noi variante semnate.',
      etichetaConfirmare: 'Șterge',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '520px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    try {
      await this.api.stergeSemnat(this.sedintaId());
      this.procesVerbal.update(pv => pv ? {
        ...pv,
        numeFisierSemnat: null,
        marimeSemnat: null,
        dataIncarcareSemnat: null
      } : null);
      this.snackBar.open('Varianta semnată a fost ștearsă.', 'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }
}