import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { extrageMesajEroare } from '../../../core/http/erori';
import { TipDocument, TipDocumentHcl } from '../../../shared/enums';
import { etichetaTipDocumentHcl } from '../../../shared/etichete';
import {
  Document, denumireDinNumeFisier, formateazaMarime, valideazaFisier
} from '../../documente/documente.models';
import { DocumenteService } from '../../documente/documente.service';

export interface DateAnexaDialog {
  hclId: number;
  document?: Document;
  ordineDefault: number;
  numarOrdinBlocat: boolean;   // HCL semnat → tip + nr. anexă fixate (mirror gard PUT)
}

@Component({
  selector: 'app-anexa-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule
  ],
  templateUrl: './anexa-dialog.html',
  styleUrl: './anexa-dialog.scss'
})
export class AnexaDialog {
  private readonly api = inject(DocumenteService);
  private readonly dialogRef = inject(MatDialogRef<AnexaDialog, Document>);
  private readonly date = inject<DateAnexaDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly esteEditare = !!this.date.document;
  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly fisierSelectat = signal<File | null>(null);
  readonly eroareFisier = signal<string | null>(null);

  readonly etichetaTip = etichetaTipDocumentHcl;
  readonly formateazaMarime = formateazaMarime;
  readonly tipuri: TipDocumentHcl[] = [
    TipDocumentHcl.Anexa, TipDocumentHcl.RaportSpecialitate, TipDocumentHcl.ExpunereDeMotive,
    TipDocumentHcl.AvizComisie, TipDocumentHcl.Justificativ, TipDocumentHcl.Altul
  ];

  readonly form = this.fb.nonNullable.group({
    denumire: [this.date.document?.denumire ?? '', Validators.required],
    descriere: [this.date.document?.descriere ?? ''],
    tipDocumentHcl: [this.date.document?.tipDocumentHcl ?? TipDocumentHcl.Anexa, Validators.required],
    numarOrdinAnexa: [this.date.document?.numarOrdinAnexa ?? null as number | null],
    ordine: [this.date.document?.ordine ?? this.date.ordineDefault, [Validators.required, Validators.min(0)]]
  });

  readonly tipCurent = toSignal(this.form.controls.tipDocumentHcl.valueChanges,
    { initialValue: this.form.controls.tipDocumentHcl.value });
  readonly esteAnexa = computed(() => this.tipCurent() === TipDocumentHcl.Anexa);
  // La editarea unui HCL semnat, tipul și numărul anexei sunt imutabile (referențiate în corp).
  readonly numarBlocat = computed(() => this.esteEditare && this.date.numarOrdinBlocat);

  constructor() {
    if (this.numarBlocat()) {
      this.form.controls.tipDocumentHcl.disable();
      this.form.controls.numarOrdinAnexa.disable();
    }
  }

  laFisierSelectat(event: Event): void {
    const input = event.target as HTMLInputElement;
    const fisier = input.files?.[0];
    if (!fisier) {
      this.fisierSelectat.set(null);
      this.eroareFisier.set(null);
      return;
    }
    const eroare = valideazaFisier(fisier.name, fisier.size);
    if (eroare) {
      this.fisierSelectat.set(null);
      this.eroareFisier.set(eroare);
      input.value = '';
      return;
    }
    this.fisierSelectat.set(fisier);
    this.eroareFisier.set(null);
    if (!this.form.controls.denumire.dirty || !this.form.controls.denumire.value) {
      this.form.controls.denumire.setValue(denumireDinNumeFisier(fisier.name));
    }
  }

  async salveaza(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (!this.esteEditare && !this.fisierSelectat()) {
      this.eroareFisier.set('Selectează un fișier.');
      return;
    }

    const v = this.form.getRawValue();
    if (v.tipDocumentHcl === TipDocumentHcl.Anexa && v.numarOrdinAnexa == null) {
      this.eroare.set('Pentru o anexă, numărul de ordine este obligatoriu.');
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);
    const denumire = v.denumire.trim();
    const descriere = v.descriere.trim() || null;
    const numarOrdinAnexa = v.tipDocumentHcl === TipDocumentHcl.Anexa ? v.numarOrdinAnexa : null;

    try {
      const rezultat = this.esteEditare
        ? await this.api.actualizeaza(this.date.document!.id, {
            denumire, descriere,
            tipDocument: TipDocument.Altele,   // ignorat pe HCL — sursa de adevăr e tipDocumentHcl
            ordine: v.ordine,
            tipDocumentHcl: v.tipDocumentHcl,
            numarOrdinAnexa
          })
        : await this.api.incarca({
            fisier: this.fisierSelectat()!,
            denumire,
            tipDocument: TipDocument.Altele,
            descriere,
            ordine: v.ordine,
            context: { hclId: this.date.hclId },
            tipDocumentHcl: v.tipDocumentHcl,
            numarOrdinAnexa
          });
      this.dialogRef.close(rezultat);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }
}
