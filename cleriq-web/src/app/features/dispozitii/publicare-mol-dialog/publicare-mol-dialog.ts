import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { extrageMesajEroare } from '../../../core/http/erori';
import { TipDispozitie } from '../../../shared/enums';
import { AVERTISMENT_PUBLICARE_INDIVIDUALA } from '../../../shared/text';
import { DispozitieDetalii } from '../dispozitii.models';
import { DispozitiiService } from '../dispozitii.service';

export interface DatePublicareMolDispozitieDialog {
  dispozitieId: number;
  tip: TipDispozitie;
}

@Component({
  selector: 'app-publicare-mol-dispozitie-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatProgressSpinnerModule, MatIconModule
  ],
  templateUrl: './publicare-mol-dialog.html',
  styleUrl: './publicare-mol-dialog.scss'
})
export class PublicareMolDispozitieDialog {
  private readonly api = inject(DispozitiiService);
  private readonly dialogRef = inject(MatDialogRef<PublicareMolDispozitieDialog, DispozitieDetalii>);
  private readonly date = inject<DatePublicareMolDispozitieDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);

  // Individual → publicarea în MOL e un override deliberat: avertisment GDPR + motiv obligatoriu.
  readonly esteIndividual = this.date.tip === TipDispozitie.Individual;
  readonly avertismentIndividual = AVERTISMENT_PUBLICARE_INDIVIDUALA;

  readonly form = this.fb.nonNullable.group({
    data: [this.dataAzi(), Validators.required],
    motiv: ['', Validators.maxLength(1000)]
  });

  constructor() {
    if (this.esteIndividual) {
      this.form.controls.motiv.addValidators(Validators.required);
      this.form.controls.motiv.updateValueAndValidity();
    }
  }

  async publica(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.seSalveaza.set(true);
    this.eroare.set(null);
    try {
      // Data publicării = DateOnly „yyyy-MM-dd", direct din <input type="date">.
      const motiv = this.esteIndividual ? this.form.controls.motiv.value.trim() : null;
      const rezultat = await this.api.publicaMol(
        this.date.dispozitieId, this.form.controls.data.value, this.esteIndividual, motiv);
      this.dialogRef.close(rezultat);
    } catch (err) {
      // Plasă defensivă: soft-409 { mesaj, necesitaConfirmarePublicareIndividuala } → mesaj inline.
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }

  private dataAzi(): string {
    return new Date().toISOString().substring(0, 10);
  }
}
