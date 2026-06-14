import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { extrageMesajEroare } from '../../../core/http/erori';
import { TipDocument } from '../../../shared/enums';
import { etichetaTipDocument } from '../../../shared/etichete';
import {
  ContextDocument, DocumenteService
} from '../documente.service';
import {
  Document, deduceTipDocument, denumireDinNumeFisier,
  formateazaMarime, valideazaFisier
} from '../documente.models';

export interface DateDocumentDialog {
  document?: Document;
  context: ContextDocument;
  ordineDefault: number;
}

@Component({
  selector: 'app-document-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatSelectModule, MatIconModule
  ],
  templateUrl: './document-dialog.html',
  styleUrl: './document-dialog.scss'
})
export class DocumentDialog {
  private readonly api = inject(DocumenteService);
  private readonly dialogRef = inject(MatDialogRef<DocumentDialog>);
  private readonly date = inject<DateDocumentDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly esteEditare = !!this.date.document;
  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly fisierSelectat = signal<File | null>(null);
  readonly eroareFisier = signal<string | null>(null);

  readonly tipuriDocument = [
    TipDocument.ProiectHCL, TipDocument.ExpunereDeMotive, TipDocument.Aviz,
    TipDocument.Raport, TipDocument.Anexa, TipDocument.Altele
  ];

  readonly etichetaTipDocument = etichetaTipDocument;
  readonly formateazaMarime = formateazaMarime;

  readonly form = this.fb.nonNullable.group({
    denumire: [this.date.document?.denumire ?? '', Validators.required],
    descriere: [this.date.document?.descriere ?? ''],
    tipDocument: [
      this.date.document?.tipDocument ?? TipDocument.Anexa,
      Validators.required
    ],
    ordine: [
      this.date.document?.ordine ?? this.date.ordineDefault,
      [Validators.required, Validators.min(0)]
    ]
  });

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
    if (!this.form.controls.tipDocument.dirty) {
      this.form.controls.tipDocument.setValue(deduceTipDocument(fisier.name));
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

    this.seSalveaza.set(true);
    this.eroare.set(null);

    const v = this.form.getRawValue();
    const denumire = v.denumire.trim();
    const descriere = v.descriere.trim() || null;

    try {
      const rezultat = this.esteEditare
        ? await this.api.actualizeaza(this.date.document!.id, {
            denumire, descriere,
            tipDocument: v.tipDocument,
            ordine: v.ordine
          })
        : await this.api.incarca({
            fisier: this.fisierSelectat()!,
            denumire,
            tipDocument: v.tipDocument,
            descriere,
            ordine: v.ordine,
            context: this.date.context
          });
      this.dialogRef.close(rezultat);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }
}