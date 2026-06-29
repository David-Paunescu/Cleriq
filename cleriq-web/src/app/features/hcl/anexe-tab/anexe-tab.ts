import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
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
import { StatusHclRedactional } from '../../../shared/enums';
import { etichetaTipDocumentHcl } from '../../../shared/etichete';
import { Document, formateazaMarime } from '../../documente/documente.models';
import { DocumenteService } from '../../documente/documente.service';
import { AnexaDialog, DateAnexaDialog } from '../anexa-dialog/anexa-dialog';
import { actiuniAnexe } from '../hcl.permisiuni';

const COLOANE = ['ordin', 'denumire', 'tip', 'marime'];

@Component({
  selector: 'app-anexe-tab',
  imports: [
    MatTableModule, MatIconModule, MatButtonModule,
    MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './anexe-tab.html',
  styleUrl: './anexe-tab.scss'
})
export class AnexeTab implements OnInit {
  private readonly api = inject(DocumenteService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly hclId = input.required<number>();
  readonly status = input.required<StatusHclRedactional>();

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly anexe = signal<Document[]>([]);
  readonly randuriBlocate = signal<ReadonlySet<number>>(new Set());

  readonly actiuni = computed(() =>
    actiuniAnexe(this.status(), this.auth.areOricareRol('Admin', 'Secretar')));

  readonly coloane = computed(() =>
    this.actiuni().poateEdita || this.actiuni().poateSterge ? [...COLOANE, 'actiuni'] : COLOANE);

  readonly etichetaTip = etichetaTipDocumentHcl;
  readonly formateazaMarime = formateazaMarime;

  ngOnInit(): void {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      const lista = await this.api.lista({ hclId: this.hclId() });
      // Anexele numerotate primele (după nr. ordine), apoi celelalte documente după ordine/creare.
      this.anexe.set([...lista].sort((a, b) =>
        (a.numarOrdinAnexa ?? Number.MAX_SAFE_INTEGER) - (b.numarOrdinAnexa ?? Number.MAX_SAFE_INTEGER)
        || a.ordine - b.ordine
        || a.creatLa.localeCompare(b.creatLa)));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  async adauga(): Promise<void> {
    if (!this.actiuni().poateAdauga) return;
    const ordineDefault = this.anexe().length === 0
      ? 0 : Math.max(...this.anexe().map(d => d.ordine)) + 1;
    const data: DateAnexaDialog = {
      hclId: this.hclId(), ordineDefault, numarOrdinBlocat: this.actiuni().numarOrdinBlocat
    };
    const rezultat = await firstValueFrom(
      this.dialog.open<AnexaDialog, DateAnexaDialog, Document | undefined>(
        AnexaDialog, { data, width: '560px', maxWidth: '95vw' }).afterClosed());
    if (!rezultat) return;
    this.snackBar.open('Anexa a fost încărcată.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  async editeaza(d: Document): Promise<void> {
    if (!this.actiuni().poateEdita) return;
    const data: DateAnexaDialog = {
      hclId: this.hclId(), document: d, ordineDefault: d.ordine,
      numarOrdinBlocat: this.actiuni().numarOrdinBlocat
    };
    const rezultat = await firstValueFrom(
      this.dialog.open<AnexaDialog, DateAnexaDialog, Document | undefined>(
        AnexaDialog, { data, width: '560px', maxWidth: '95vw' }).afterClosed());
    if (!rezultat) return;
    this.snackBar.open('Anexa a fost actualizată.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  async sterge(d: Document): Promise<void> {
    if (!this.actiuni().poateSterge || this.randuriBlocate().has(d.id)) return;
    const data: DateConfirmare = {
      titlu: 'Ștergere anexă',
      mesaj: `Ștergi „${d.denumire}"? Acțiunea nu poate fi anulată.`,
      etichetaConfirmare: 'Șterge',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data, width: '480px', maxWidth: '95vw' }).afterClosed());
    if (!confirmat) return;

    this.blocheazaRand(d.id);
    try {
      await this.api.sterge(d.id);
      this.snackBar.open('Anexa a fost ștearsă.', 'Închide', { duration: 3000 });
      await this.incarca();
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.deblocheazaRand(d.id);
    }
  }

  async descarca(d: Document): Promise<void> {
    if (this.randuriBlocate().has(d.id)) return;
    this.blocheazaRand(d.id);
    try {
      await this.api.descarca(d.id, d.numeFisierOriginal);
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.deblocheazaRand(d.id);
    }
  }

  esteRandBlocat(id: number): boolean {
    return this.randuriBlocate().has(id);
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
