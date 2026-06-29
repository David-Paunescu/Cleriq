import {
  Component, ElementRef, computed, effect, inject, input, output, signal, viewChild
} from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { extrageMesajEroare } from '../../../core/http/erori';
import { ConfirmareDialog, DateConfirmare } from '../../../shared/confirmare/confirmare-dialog';
import { formateazaDataOra } from '../../../shared/data';
import { RolSemnatar, StatusHclRedactional } from '../../../shared/enums';
import { etichetaRolSemnatar } from '../../../shared/etichete';
import { HclDetalii, SemnatarHcl, formateazaMarime } from '../hcl.models';
import { ActiuniHcl } from '../hcl.permisiuni';
import { HclService } from '../hcl.service';
import { DateSemnatarDialog, SemnatarDialog } from '../semnatar-dialog/semnatar-dialog';

@Component({
  selector: 'app-semnatari-tab',
  imports: [
    MatCardModule, MatButtonModule, MatIconModule, MatMenuModule,
    MatTooltipModule, MatProgressSpinnerModule
  ],
  templateUrl: './semnatari-tab.html',
  styleUrl: './semnatari-tab.scss'
})
export class SemnatariTab {
  private readonly api = inject(HclService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly hcl = input.required<HclDetalii>();
  readonly actiuni = input.required<ActiuniHcl>();
  readonly actualizat = output<HclDetalii>();

  readonly seStergeId = signal<number | null>(null);
  readonly seSalveazaMotiv = signal(false);
  readonly seIncarcaSemnat = signal(false);
  readonly seDescarcaSemnat = signal(false);

  readonly motivEditor = signal('');
  private ultimulMotivServer: string | null = null;
  readonly textareaMotiv = viewChild<ElementRef<HTMLTextAreaElement>>('textareaMotiv');
  readonly inputFisierSemnat = viewChild<ElementRef<HTMLInputElement>>('inputFisierSemnat');

  readonly etichetaRolSemnatar = etichetaRolSemnatar;
  readonly formateazaDataOra = formateazaDataOra;
  readonly formateazaMarime = formateazaMarime;

  readonly esteSemnatStatus = computed(() => this.hcl().status === StatusHclRedactional.Semnat);
  readonly arePresedinte = computed(() =>
    this.hcl().semnatari.some(s => s.rolSemnatar === RolSemnatar.PresedinteSedinta));
  readonly afiseazaMotiv = computed(() =>
    this.actiuni().poateGestionaSemnatari
    || (this.hcl().motivLipsaSemnaturaPresedinte?.trim().length ?? 0) > 0);
  readonly motivDirty = computed(() =>
    this.motivEditor().trim() !== (this.hcl().motivLipsaSemnaturaPresedinte ?? '').trim());
  readonly art140Indeplinit = computed(() => {
    const alternativi = this.hcl().semnatari.filter(
      s => s.rolSemnatar === RolSemnatar.SemnatarAlternativArt140).length;
    const areMotiv = (this.hcl().motivLipsaSemnaturaPresedinte?.trim().length ?? 0) > 0;
    return alternativi >= 2 && areMotiv;
  });

  constructor() {
    // Resetează editorul când motivul de pe server se schimbă (load / save / auto-clear la delete).
    effect(() => {
      const motivServer = this.hcl().motivLipsaSemnaturaPresedinte ?? '';
      if (motivServer !== this.ultimulMotivServer) {
        this.ultimulMotivServer = motivServer;
        this.motivEditor.set(motivServer);
      }
    });
    // Sync DOM ← signal (cursor protejat), paritar editorului de conținut.
    effect(() => {
      const v = this.motivEditor();
      const ref = this.textareaMotiv();
      if (ref && ref.nativeElement.value !== v) ref.nativeElement.value = v;
    });
  }

  laInputMotiv(e: Event): void {
    this.motivEditor.set((e.target as HTMLTextAreaElement).value);
  }

  async deschideAdaugare(): Promise<void> {
    if (!this.actiuni().poateGestionaSemnatari) return;
    const max = this.hcl().semnatari.reduce((m, s) => Math.max(m, s.ordineAfisare), 0);
    const data: DateSemnatarDialog = { hclId: this.hcl().id, ordineAfisareSugerata: max + 1 };
    const rezultat = await firstValueFrom(
      this.dialog.open<SemnatarDialog, DateSemnatarDialog, HclDetalii | undefined>(
        SemnatarDialog, { data, width: '460px', maxWidth: '95vw' }).afterClosed());
    if (!rezultat) return;
    this.actualizat.emit(rezultat);
    this.snackBar.open('Semnatarul a fost adăugat.', 'Închide', { duration: 3000 });
  }

  async stergeSemnatar(s: SemnatarHcl): Promise<void> {
    if (!this.actiuni().poateGestionaSemnatari || this.seStergeId() !== null) return;
    this.seStergeId.set(s.id);
    try {
      const rezultat = await this.api.stergeSemnatar(this.hcl().id, s.id);
      this.actualizat.emit(rezultat);
      this.snackBar.open('Semnatarul a fost eliminat.', 'Închide', { duration: 3000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.seStergeId.set(null);
    }
  }

  async salveazaMotiv(): Promise<void> {
    if (!this.actiuni().poateGestionaSemnatari || this.seSalveazaMotiv() || !this.motivDirty()) return;
    const motiv = this.motivEditor().trim();
    if (motiv.length === 0) {
      this.snackBar.open('Motivul nu poate fi gol.', 'Închide', { duration: 4000 });
      return;
    }
    this.seSalveazaMotiv.set(true);
    try {
      const rezultat = await this.api.seteazaMotivLipsa(this.hcl().id, motiv);
      this.actualizat.emit(rezultat);
      this.snackBar.open('Motivul a fost salvat.', 'Închide', { duration: 3000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.seSalveazaMotiv.set(false);
    }
  }

  // === Variantă semnată (PDF scanat) ===
  incepIncarcareSemnat(): void {
    if (!this.actiuni().poateIncarcaSemnat || this.seIncarcaSemnat()) return;
    this.inputFisierSemnat()?.nativeElement.click();
  }

  async incepInlocuireSemnat(): Promise<void> {
    if (!this.actiuni().poateInlocuiSemnat || this.seIncarcaSemnat()) return;
    const date: DateConfirmare = {
      titlu: 'Înlocuire variantă semnată',
      mesaj: 'Varianta semnată curentă va fi înlocuită cu noul fișier PDF.',
      etichetaConfirmare: 'Continuă'
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '480px', maxWidth: '95vw' }).afterClosed());
    if (!confirmat) return;
    this.inputFisierSemnat()?.nativeElement.click();
  }

  async laFisierSemnatSelectat(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const fisier = input.files?.[0];
    input.value = '';
    if (!fisier) return;
    if (!fisier.name.toLowerCase().endsWith('.pdf')) {
      this.snackBar.open('Doar fișiere PDF sunt acceptate.', 'Închide', { duration: 5000 });
      return;
    }
    if (fisier.size === 0) {
      this.snackBar.open('Fișierul este gol.', 'Închide', { duration: 5000 });
      return;
    }
    this.seIncarcaSemnat.set(true);
    try {
      const rezultat = await this.api.incarcaSemnat(this.hcl().id, fisier);
      this.actualizat.emit(rezultat);
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
      const h = this.hcl();
      const nume = h.numar != null
        ? `hcl-${h.numar}-${h.anNumerotare}-semnat.pdf`
        : `hcl-${h.id}-semnat.pdf`;
      await this.api.descarcaSemnat(h.id, nume);
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
      mesaj: 'Varianta semnată va fi eliminată definitiv din această hotărâre.',
      etichetaConfirmare: 'Șterge',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '520px', maxWidth: '95vw' }).afterClosed());
    if (!confirmat) return;
    try {
      await this.api.stergeSemnat(this.hcl().id);
      // DELETE întoarce NoContent → patch local (paritar ProcesVerbalTab.stergeSemnat).
      this.actualizat.emit({
        ...this.hcl(),
        esteSemnat: false, numeFisierSemnat: null, marimeSemnat: null, dataIncarcareSemnat: null
      });
      this.snackBar.open('Varianta semnată a fost ștearsă.', 'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }
}
