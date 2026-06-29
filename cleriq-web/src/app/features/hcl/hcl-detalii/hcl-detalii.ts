import {
  Component, ElementRef, OnDestroy, OnInit, computed, effect, inject, signal, viewChild
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
import {
  ModificariNesalvateService, ProprietarStareModificari
} from '../../../core/modificari/modificari-nesalvate.service';
import { ConfirmareDialog, DateConfirmare } from '../../../shared/confirmare/confirmare-dialog';
import { formateazaDataOra } from '../../../shared/data';
import { StatusHclRedactional } from '../../../shared/enums';
import { etichetaStatusHcl, etichetaTipHcl, etichetaTipMajoritate } from '../../../shared/etichete';
import {
  AtribuieNumarDialog, DateAtribuireNumarDialog
} from '../atribuie-numar-dialog/atribuie-numar-dialog';
import { DateInvalidareDialog, InvalidareDialog } from '../invalidare-dialog/invalidare-dialog';
import {
  DatePublicareMolDialog, PublicareMolDialog
} from '../publicare-mol-dialog/publicare-mol-dialog';
import { SemnatariTab } from '../semnatari-tab/semnatari-tab';
import { ComunicariTab } from '../comunicari-tab/comunicari-tab';
import { RelatiiTab } from '../relatii-tab/relatii-tab';
import { AnexeTab } from '../anexe-tab/anexe-tab';
import { HclDetalii } from '../hcl.models';
import { actiuniPermise } from '../hcl.permisiuni';
import { HclService } from '../hcl.service';

const DEBOUNCE_AUTOSAVE_MS = 2000;

@Component({
  selector: 'app-hcl-detalii',
  imports: [
    MatCardModule, MatTabsModule, MatIconModule, MatButtonModule,
    MatMenuModule, MatTooltipModule, MatProgressSpinnerModule, SemnatariTab, ComunicariTab,
    RelatiiTab, AnexeTab
  ],
  templateUrl: './hcl-detalii.html',
  styleUrl: './hcl-detalii.scss'
})
export class HclDetaliiPagina implements OnInit, OnDestroy {
  private readonly api = inject(HclService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly modificari = inject(ModificariNesalvateService);

  id = Number(this.route.snapshot.paramMap.get('id'));

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly hcl = signal<HclDetalii | null>(null);
  readonly indexTabActiv = signal(Number(this.route.snapshot.queryParamMap.get('tab')) || 0);

  readonly seSemneaza = signal(false);
  readonly seDescarcaPdf = signal(false);
  readonly actiuneStareLegala = signal(false);

  // === Editor (paritate ProcesVerbalTab) ===
  readonly valoareEditor = signal('');
  readonly ultimaValoareSalvata = signal('');
  readonly valoareEditorInitializata = signal(false);
  readonly dataUltimeiSalvari = signal<Date | null>(null);
  readonly seSalveaza = signal(false);
  readonly eroareSalvare = signal<string | null>(null);
  readonly dirtyPersistent = signal(false);
  readonly actiuneInCurs = signal(false);

  readonly textareaEditor = viewChild<ElementRef<HTMLTextAreaElement>>('textareaEditor');

  private vizibilitateHandler: (() => void) | null = null;
  private proprietar!: ProprietarStareModificari;

  readonly esteAdmin = computed(() => this.auth.areRol('Admin'));
  readonly esteAdminSauSecretar = computed(() => this.auth.areOricareRol('Admin', 'Secretar'));
  readonly actiuni = computed(() =>
    actiuniPermise(this.hcl(), this.esteAdminSauSecretar(), this.esteAdmin()));

  readonly esteDirty = computed(() => this.valoareEditor() !== this.ultimaValoareSalvata());

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

  readonly StatusHclRedactional = StatusHclRedactional;
  readonly etichetaStatusHcl = etichetaStatusHcl;
  readonly etichetaTipHcl = etichetaTipHcl;
  readonly etichetaTipMajoritate = etichetaTipMajoritate;
  readonly formateazaDataOra = formateazaDataOra;

  private readonly handlerKeydown = (event: KeyboardEvent): void => {
    if (this.indexTabActiv() !== 1) return;  // doar pe tab Conținut
    if (!(event.ctrlKey || event.metaKey)) return;
    if (event.key.toLowerCase() !== 's') return;
    event.preventDefault();
    if (this.actiuni().poateEditaContinut && !this.actiuneInCurs()) {
      this.salveazaImediat(false);
    }
  };

  constructor() {
    // Inițializare editor din conținut — o singură dată.
    effect(() => {
      const h = this.hcl();
      if (h && !this.valoareEditorInitializata()) {
        const v = h.continut ?? '';
        this.valoareEditor.set(v);
        this.ultimaValoareSalvata.set(v);
        this.valoareEditorInitializata.set(true);
      }
    });

    // Sync DOM ← signal (cursor protejat).
    effect(() => {
      const v = this.valoareEditor();
      const ref = this.textareaEditor();
      if (ref && ref.nativeElement.value !== v) {
        ref.nativeElement.value = v;
      }
    });

    // Auto-save 2s + semnal „dirty persistent" la 10s.
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

    // Reîncarcă la schimbarea :id (navigare HCL→HCL: relații, registru), fiindcă
    // Angular reutilizează componenta pe aceeași rută /hcl/:id.
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe(params => {
      this.initPentruHcl(Number(params.get('id')));
    });
  }

  ngOnInit(): void {
    this.vizibilitateHandler = () => {
      if (document.visibilityState === 'hidden'
          && this.esteDirty() && !this.seSalveaza() && !this.actiuneInCurs()) {
        this.salveazaImediat(true);
      }
    };
    document.addEventListener('visibilitychange', this.vizibilitateHandler);
    window.addEventListener('keydown', this.handlerKeydown);
  }

  private initPentruHcl(idNou: number): void {
    this.id = idNou;
    this.hcl.set(null);
    this.eroare.set(null);
    this.valoareEditor.set('');
    this.ultimaValoareSalvata.set('');
    this.valoareEditorInitializata.set(false);
    this.eroareSalvare.set(null);
    this.dataUltimeiSalvari.set(null);
    this.indexTabActiv.set(Number(this.route.snapshot.queryParamMap.get('tab')) || 0);
    if (this.proprietar) this.modificari.retragere(this.proprietar.id);
    this.proprietar = {
      id: `hcl-continut-${idNou}`,
      areModificariNesalvate: () => this.esteDirty()
    };
    this.modificari.inregistreaza(this.proprietar);
    this.incarca();
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
      this.hcl.set(await this.api.detalii(this.id));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
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
      const rezultat = await this.api.editeazaContinut(this.id, { continut: valoare });
      this.hcl.set(rezultat);
      this.ultimaValoareSalvata.set(valoare);

      if (!this.esteDirty()) {
        const ramas = 600 - (Date.now() - inceput);
        if (ramas > 0) await new Promise(r => setTimeout(r, ramas));
      }

      this.dataUltimeiSalvari.set(new Date());
      if (!silent) this.snackBar.open('Conținutul a fost salvat.', 'Închide', { duration: 3000 });
    } catch (err) {
      const mesaj = extrageMesajEroare(err);
      this.eroareSalvare.set(mesaj);
      if (!silent) this.snackBar.open(mesaj, 'Închide', { duration: 5000 });
    } finally {
      this.seSalveaza.set(false);
    }
  }

  async regenereaza(): Promise<void> {
    if (!this.actiuni().poateRegenera || this.actiuneInCurs()) return;

    if (this.esteDirty()) {
      this.snackBar.open(
        'Așteaptă salvarea automată sau apasă Ctrl+S înainte de regenerare.',
        'Închide', { duration: 5000 });
      return;
    }

    const date: DateConfirmare = {
      titlu: 'Regenerare conținut',
      mesaj: 'Toate editările manuale vor fi pierdute definitiv. Conținutul va fi regenerat din datele actuale ale hotărârii (titlu, vot, semnatari).',
      etichetaConfirmare: 'Regenerează',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '520px', maxWidth: '95vw' }).afterClosed());
    if (!confirmat) return;

    this.actiuneInCurs.set(true);
    try {
      const rezultat = await this.api.regenereazaContinut(this.id);
      this.adoptaContinut(rezultat);
      this.dataUltimeiSalvari.set(null);
      this.eroareSalvare.set(null);
      this.snackBar.open('Conținutul a fost regenerat.', 'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.actiuneInCurs.set(false);
    }
  }

  // Adoptă conținutul venit server-side (regenerare / numerotare) în editor, suprascriind valoarea.
  private adoptaContinut(h: HclDetalii): void {
    const v = h.continut ?? '';
    this.valoareEditor.set(v);
    this.ultimaValoareSalvata.set(v);
    this.hcl.set(h);
  }

  async deschideAtribuieNumar(): Promise<void> {
    if (!this.actiuni().poateAtribuiNumar) return;

    // Numerotarea înlocuiește placeholderul în conținutul STOCAT → salvăm editările întâi.
    if (this.esteDirty()) {
      await this.salveazaImediat(true);
      if (this.eroareSalvare() || this.esteDirty()) {
        this.snackBar.open('Salvează conținutul înainte de a atribui numărul.', 'Închide', { duration: 5000 });
        return;
      }
    }

    const date: DateAtribuireNumarDialog = { hclId: this.id };
    const rezultat = await firstValueFrom(
      this.dialog.open<AtribuieNumarDialog, DateAtribuireNumarDialog, HclDetalii | undefined>(
        AtribuieNumarDialog, { data: date, width: '440px', maxWidth: '95vw' }).afterClosed());
    if (!rezultat) return;

    this.adoptaContinut(rezultat);  // conținut cu numărul înlocuit + status Numerotat
    this.snackBar.open(
      `Hotărârea a primit numărul ${rezultat.numar}/${rezultat.anNumerotare}.`,
      'Închide', { duration: 4000 });
  }

  async semneaza(): Promise<void> {
    if (!this.actiuni().poateSemna || this.actiuneInCurs()) return;

    if (this.esteDirty()) {
      await this.salveazaImediat(true);
      if (this.eroareSalvare() || this.esteDirty()) {
        this.snackBar.open('Salvarea modificărilor a eșuat. Semnarea a fost anulată.', 'Închide', { duration: 5000 });
        return;
      }
    }

    const date: DateConfirmare = {
      titlu: 'Semnare hotărâre',
      mesaj: 'După semnare, hotărârea devine act juridic finalizat: conținutul nu mai poate fi editat sau regenerat.',
      etichetaConfirmare: 'Semnează',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '520px', maxWidth: '95vw' }).afterClosed());
    if (!confirmat) return;

    this.seSemneaza.set(true);
    this.actiuneInCurs.set(true);
    try {
      const rezultat = await this.api.semneaza(this.id);
      this.hcl.set(rezultat);
      this.snackBar.open('Hotărârea a fost semnată.', 'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.seSemneaza.set(false);
      this.actiuneInCurs.set(false);
    }
  }

  async descarcaPdf(): Promise<void> {
    if (!this.actiuni().poateDescarcaPdf || this.seDescarcaPdf()) return;

    this.seDescarcaPdf.set(true);
    try {
      const h = this.hcl()!;
      const nume = h.numar != null
        ? `hcl-${h.numar}-${h.anNumerotare}.pdf`
        : `hcl-draft-${h.id}.pdf`;
      await this.api.descarcaPdf(this.id, nume);
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.seDescarcaPdf.set(false);
    }
  }

  // === FE2 — stări legale (antet) ===

  async comutaPublicare(): Promise<void> {
    const act = this.actiuni();
    if (this.actiuneStareLegala()) return;

    if (act.poatePublica) {
      await this.executaStareLegala(
        () => this.api.publica(this.id, true), 'Hotărârea a fost publicată pe portal.');
      return;
    }
    if (act.poateDepublica) {
      const confirmat = await this.confirma({
        titlu: 'Retragere de pe portal',
        mesaj: 'Hotărârea nu va mai fi vizibilă pe portalul public. O poți republica oricând.',
        etichetaConfirmare: 'Retrage',
        periculos: true
      });
      if (!confirmat) return;
      await this.executaStareLegala(
        () => this.api.publica(this.id, false), 'Hotărârea a fost retrasă de pe portal.');
    }
  }

  async deschidePublicareMol(): Promise<void> {
    if (!this.actiuni().poatePublicaMol) return;
    const date: DatePublicareMolDialog = { hclId: this.id };
    const rezultat = await firstValueFrom(
      this.dialog.open<PublicareMolDialog, DatePublicareMolDialog, HclDetalii | undefined>(
        PublicareMolDialog, { data: date, width: '440px', maxWidth: '95vw' }).afterClosed());
    if (!rezultat) return;
    this.hcl.set(rezultat);
    this.snackBar.open('Hotărârea a fost publicată în MOL.', 'Închide', { duration: 4000 });
  }

  async anuleazaMol(): Promise<void> {
    if (!this.actiuni().poateAnulaMol || this.actiuneStareLegala()) return;
    const confirmat = await this.confirma({
      titlu: 'Anulare publicare MOL',
      mesaj: 'Se anulează data publicării în Monitorul Oficial Local. Folosit doar pentru corecții administrative.',
      etichetaConfirmare: 'Anulează publicarea MOL',
      periculos: true
    });
    if (!confirmat) return;
    await this.executaStareLegala(
      () => this.api.anuleazaMol(this.id), 'Publicarea în MOL a fost anulată.');
  }

  async deschideInvalidare(): Promise<void> {
    if (!this.actiuni().poateInvalida) return;
    const date: DateInvalidareDialog = { hclId: this.id };
    const rezultat = await firstValueFrom(
      this.dialog.open<InvalidareDialog, DateInvalidareDialog, HclDetalii | undefined>(
        InvalidareDialog, { data: date, width: '520px', maxWidth: '95vw' }).afterClosed());
    if (!rezultat) return;
    this.hcl.set(rezultat);
    this.snackBar.open('Hotărârea a fost invalidată.', 'Închide', { duration: 4000 });
  }

  async anuleazaInvalidare(): Promise<void> {
    if (!this.actiuni().poateAnulaInvalidare || this.actiuneStareLegala()) return;
    const confirmat = await this.confirma({
      titlu: 'Anulare invalidare',
      mesaj: 'Hotărârea revine în vigoare (se șterg motivul și referința invalidării). Folosit doar pentru corecții administrative.',
      etichetaConfirmare: 'Anulează invalidarea',
      periculos: true
    });
    if (!confirmat) return;
    await this.executaStareLegala(
      () => this.api.anuleazaInvalidare(this.id), 'Invalidarea a fost anulată.');
  }

  private async executaStareLegala(
    actiune: () => Promise<HclDetalii>, mesajSucces: string): Promise<void> {
    this.actiuneStareLegala.set(true);
    try {
      this.hcl.set(await actiune());
      this.snackBar.open(mesajSucces, 'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.actiuneStareLegala.set(false);
    }
  }

  private async confirma(date: DateConfirmare): Promise<boolean> {
    return !!(await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '520px', maxWidth: '95vw' })
        .afterClosed()));
  }

  inapoiLaLista(): void {
    this.router.navigate(['/hcl']);
  }

  laSchimbareTab(index: number): void {
    this.indexTabActiv.set(index);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab: index || null },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  numarAfisat(h: HclDetalii): string {
    return h.numar != null && h.anNumerotare != null ? `${h.numar}/${h.anNumerotare}` : '—';
  }

  claseStatus(status: StatusHclRedactional): string {
    switch (status) {
      case StatusHclRedactional.Draft: return 'badge badge-draft';
      case StatusHclRedactional.Numerotat: return 'badge badge-numerotat';
      case StatusHclRedactional.Semnat: return 'badge badge-semnat';
    }
  }
}
