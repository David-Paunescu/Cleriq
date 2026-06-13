import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { extrageMesajEroare } from '../../../core/http/erori';
import { TipMajoritate, TipPunct, TipVot } from '../../../shared/enums';
import {
  etichetaTipMajoritate, etichetaTipPunct, etichetaTipVot
} from '../../../shared/etichete';
import { ActualizarePunct, CrearePunct, PunctOrdineZi } from '../puncte.models';
import { PuncteService } from '../puncte.service';

export interface DatePunctDialog {
  sedintaId: number;
  punct?: PunctOrdineZi;
  ordineDefault?: number;
}

@Component({
  selector: 'app-puncte-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule
  ],
  templateUrl: './puncte-dialog.html',
  styleUrl: './puncte-dialog.scss'
})
export class PuncteDialog {
  private readonly api = inject(PuncteService);
  private readonly dialogRef = inject(MatDialogRef<PuncteDialog>);
  private readonly date = inject<DatePunctDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly esteEditare = !!this.date.punct;
  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);

  readonly tipuriPunct = [TipPunct.ProiectHCL, TipPunct.Informare, TipPunct.Diverse];
  readonly tipuriVot = [TipVot.Nominal, TipVot.Secret];
  readonly tipuriMajoritate = [TipMajoritate.Simpla, TipMajoritate.Absoluta, TipMajoritate.Calificata];

  readonly etichetaTipPunct = etichetaTipPunct;
  readonly etichetaTipVot = etichetaTipVot;
  readonly etichetaTipMajoritate = etichetaTipMajoritate;

  readonly form = this.fb.nonNullable.group({
    ordine: [this.date.punct?.ordine ?? this.date.ordineDefault ?? 1,
      [Validators.required, Validators.min(1)]],
    titlu: [this.date.punct?.titlu ?? '', Validators.required],
    descriere: [this.date.punct?.descriere ?? ''],
    tip: [this.date.punct?.tip ?? TipPunct.ProiectHCL, Validators.required],
    necesitaVot: [this.date.punct?.necesitaVot ?? true],
    tipVot: [this.date.punct?.tipVot ?? TipVot.Nominal, Validators.required],
    tipMajoritate: [this.date.punct?.tipMajoritate ?? TipMajoritate.Simpla, Validators.required]
  });

  readonly necesitaVot = toSignal(
    this.form.controls.necesitaVot.valueChanges,
    { initialValue: this.form.controls.necesitaVot.value }
  );

  constructor() {
    this.form.controls.necesitaVot.valueChanges.subscribe(necesita => {
      this.actualizeazaValidari(!!necesita);
    });
    if (!this.form.controls.necesitaVot.value) {
      this.actualizeazaValidari(false);
    }
  }

  private actualizeazaValidari(necesita: boolean): void {
    if (necesita) {
      this.form.controls.tipVot.setValidators(Validators.required);
      this.form.controls.tipMajoritate.setValidators(Validators.required);
    } else {
      this.form.controls.tipVot.clearValidators();
      this.form.controls.tipMajoritate.clearValidators();
    }
    this.form.controls.tipVot.updateValueAndValidity();
    this.form.controls.tipMajoritate.updateValueAndValidity();
  }

  async salveaza(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);

    const v = this.form.getRawValue();
    const descriere = v.descriere.trim() || null;
    const cerere: CrearePunct = {
      ordine: v.ordine,
      titlu: v.titlu.trim(),
      descriere,
      tip: v.tip,
      necesitaVot: v.necesitaVot,
      tipVot: v.necesitaVot ? v.tipVot : null,
      tipMajoritate: v.necesitaVot ? v.tipMajoritate : null
    };

    try {
      const rezultat = this.esteEditare
        ? await this.api.actualizeaza(
            this.date.sedintaId, this.date.punct!.id, cerere as ActualizarePunct)
        : await this.api.creeaza(this.date.sedintaId, cerere);
      this.dialogRef.close(rezultat);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }
}