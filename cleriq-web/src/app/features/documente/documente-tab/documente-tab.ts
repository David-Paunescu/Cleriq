import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/auth/auth.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { ConfirmareDialog, DateConfirmare } from '../../../shared/confirmare/confirmare-dialog';
import { TipDocument } from '../../../shared/enums';
import { etichetaTipDocument } from '../../../shared/etichete';
import { normalizeazaPentruCautare } from '../../../shared/text';
import {
  DateDocumentDialog, DocumentDialog
} from '../document-dialog/document-dialog';
import { Document, formateazaMarime } from '../documente.models';
import { actiuniPermise } from '../documente.permisiuni';
import { ContextDocument, DocumenteService } from '../documente.service';

const COLOANE_BAZA = ['ordine', 'denumire', 'tip', 'marime', 'vizibilitate'];

@Component({
  selector: 'app-documente-tab',
  imports: [
    MatTableModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './documente-tab.html',
  styleUrl: './documente-tab.scss'
})
export class DocumenteTab implements OnInit {
  private readonly api = inject(DocumenteService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly sedintaId = input<number>();
  readonly punctId = input<number>();

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly documente = signal<Document[]>([]);
  readonly filtru = signal('');
  readonly filtruTip = signal<TipDocument | 'toate'>('toate');
  readonly randuriBlocate = signal<ReadonlySet<number>>(new Set());

  readonly tipuriDocument = [
    TipDocument.ProiectHCL, TipDocument.ExpunereDeMotive, TipDocument.Aviz,
    TipDocument.Raport, TipDocument.Anexa, TipDocument.Altele
  ];

  readonly actiuni = computed(() =>
    actiuniPermise(this.auth.areOricareRol('Admin', 'Secretar')));

  readonly coloane = computed(() =>
    this.actiuni().poateEdita ? [...COLOANE_BAZA, 'actiuni'] : COLOANE_BAZA);

  readonly documenteFiltrate = computed(() => {
    const termen = normalizeazaPentruCautare(this.filtru());
    const tip = this.filtruTip();
    return this.documente()
      .filter(d => tip === 'toate' || d.tipDocument === tip)
      .filter(d => !termen
        || normalizeazaPentruCautare(d.denumire).includes(termen)
        || normalizeazaPentruCautare(d.descriere ?? '').includes(termen));
  });

  readonly etichetaTipDocument = etichetaTipDocument;
  readonly formateazaMarime = formateazaMarime;

  ngOnInit(): void {
    this.incarca();
  }

  private context(): ContextDocument {
    return { sedintaId: this.sedintaId(), punctId: this.punctId() };
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      const lista = await this.api.lista(this.context());
      this.documente.set([...lista].sort((a, b) =>
        a.ordine - b.ordine || a.creatLa.localeCompare(b.creatLa)));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  laCautare(event: Event): void {
    this.filtru.set((event.target as HTMLInputElement).value);
  }

  laFiltruTip(valoare: TipDocument | 'toate'): void {
    this.filtruTip.set(valoare);
  }

  async adauga(): Promise<void> {
    const ordineDefault = this.documente().length === 0
      ? 0
      : Math.max(...this.documente().map(d => d.ordine)) + 1;
    const data: DateDocumentDialog = { context: this.context(), ordineDefault };
    const rezultat = await firstValueFrom(
      this.dialog.open(DocumentDialog, { data, width: '600px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Document încărcat.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  async editeaza(doc: Document): Promise<void> {
    const data: DateDocumentDialog = {
      document: doc, context: this.context(), ordineDefault: doc.ordine
    };
    const rezultat = await firstValueFrom(
      this.dialog.open(DocumentDialog, { data, width: '600px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Document actualizat.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  async sterge(doc: Document): Promise<void> {
    const data: DateConfirmare = {
      titlu: 'Ștergere document',
      mesaj: `Ștergi documentul „${doc.denumire}"?`,
      etichetaConfirmare: 'Șterge',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data, width: '420px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    try {
      await this.api.sterge(doc.id);
      this.snackBar.open('Document șters.', 'Închide', { duration: 4000 });
      await this.incarca();
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }

  async comutaVizibilitate(doc: Document): Promise<void> {
    if (this.randuriBlocate().has(doc.id)) return;

    if (!doc.estePublic) {
      const data: DateConfirmare = {
        titlu: 'Publicare document',
        mesaj: `Publici documentul „${doc.denumire}"? Va fi vizibil pe portalul public și va fi atașat în convocări și procese verbale.`,
        etichetaConfirmare: 'Publică'
      };
      const confirmat = await firstValueFrom(
        this.dialog.open(ConfirmareDialog, { data, width: '480px', maxWidth: '95vw' })
          .afterClosed());
      if (!confirmat) return;
    }

    this.blocheazaRand(doc.id);
    try {
      const rezultat = await this.api.seteazaVizibilitate(doc.id,
        { estePublic: !doc.estePublic });
      this.actualizeazaLocal(rezultat);
      this.snackBar.open(
        rezultat.estePublic ? 'Document publicat.' : 'Document marcat privat.',
        'Închide', { duration: 4000 });
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.deblocheazaRand(doc.id);
    }
  }

  async descarca(doc: Document): Promise<void> {
    if (this.randuriBlocate().has(doc.id)) return;
    this.blocheazaRand(doc.id);
    try {
      await this.api.descarca(doc.id, doc.numeFisierOriginal);
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.deblocheazaRand(doc.id);
    }
  }

  esteRandBlocat(id: number): boolean {
    return this.randuriBlocate().has(id);
  }

  private actualizeazaLocal(rezultat: Document): void {
    this.documente.update(lista => lista.map(d =>
      d.id === rezultat.id ? rezultat : d));
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