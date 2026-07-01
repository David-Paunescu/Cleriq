import {
  Component, ElementRef, computed, inject, input, output, signal, viewChild
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
import { StatusActRedactional } from '../../../shared/enums';
import { etichetaRolSemnatarDispozitie } from '../../../shared/etichete';
import {
  DateRefuzContrasemnareDialog, RefuzContrasemnareDialog
} from '../refuz-contrasemnare-dialog/refuz-contrasemnare-dialog';
import { DispozitieDetalii, formateazaMarime } from '../dispozitii.models';
import { ActiuniDispozitie } from '../dispozitii.permisiuni';
import { DispozitiiService } from '../dispozitii.service';

// ◆ Majoritar NOU (nu paritate directă cu HCL): dispoziția NU gestionează semnatari manual — Emitent +
// Secretar sunt derivați la creare. Reutilizăm DOAR cardul de variantă semnată; restul e read-only.
@Component({
  selector: 'app-semnatari-dispozitie-tab',
  imports: [
    MatCardModule, MatButtonModule, MatIconModule, MatMenuModule,
    MatTooltipModule, MatProgressSpinnerModule
  ],
  templateUrl: './semnatari-tab.html',
  styleUrl: './semnatari-tab.scss'
})
export class SemnatariDispozitieTab {
  private readonly api = inject(DispozitiiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly dispozitie = input.required<DispozitieDetalii>();
  readonly actiuni = input.required<ActiuniDispozitie>();
  readonly actualizat = output<DispozitieDetalii>();

  readonly seIncarcaSemnat = signal(false);
  readonly seDescarcaSemnat = signal(false);

  readonly inputFisierSemnat = viewChild<ElementRef<HTMLInputElement>>('inputFisierSemnat');

  readonly etichetaRolSemnatarDispozitie = etichetaRolSemnatarDispozitie;
  readonly formateazaDataOra = formateazaDataOra;
  readonly formateazaMarime = formateazaMarime;

  readonly esteSemnatStatus = computed(() => this.dispozitie().status === StatusActRedactional.Semnat);

  // === Refuz de contrasemnare (art. 197 alin. (3)) — acțiune one-way până la semnare ===
  async deschideRefuz(): Promise<void> {
    if (!this.actiuni().poateRefuzaContrasemnare) return;
    const date: DateRefuzContrasemnareDialog = { dispozitieId: this.dispozitie().id };
    const rezultat = await firstValueFrom(
      this.dialog.open<RefuzContrasemnareDialog, DateRefuzContrasemnareDialog, DispozitieDetalii | undefined>(
        RefuzContrasemnareDialog, { data: date, width: '560px', maxWidth: '95vw' }).afterClosed());
    if (!rezultat) return;
    this.actualizat.emit(rezultat);
    this.snackBar.open('Refuzul de contrasemnare a fost consemnat.', 'Închide', { duration: 4000 });
  }

  // === Variantă semnată (PDF scanat) — reuse pattern HCL / ProcesVerbalTab ===
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
      const rezultat = await this.api.incarcaSemnat(this.dispozitie().id, fisier);
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
      const d = this.dispozitie();
      const nume = d.numar != null
        ? `dispozitie-${d.numar}-${d.anNumerotare}-semnat.pdf`
        : `dispozitie-${d.id}-semnat.pdf`;
      await this.api.descarcaSemnat(d.id, nume);
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
      mesaj: 'Varianta semnată va fi eliminată definitiv din această dispoziție.',
      etichetaConfirmare: 'Șterge',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '520px', maxWidth: '95vw' }).afterClosed());
    if (!confirmat) return;
    try {
      await this.api.stergeSemnat(this.dispozitie().id);
      // DELETE întoarce NoContent → patch local (paritar HCL / ProcesVerbalTab).
      this.actualizat.emit({
        ...this.dispozitie(),
        esteSemnat: false, numeFisierSemnat: null, marimeSemnat: null, dataIncarcareSemnat: null
      });
      this.snackBar.open('Varianta semnată a fost ștearsă.', 'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }
}
