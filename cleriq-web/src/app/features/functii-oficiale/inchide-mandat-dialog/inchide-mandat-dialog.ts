import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { extrageMesajEroare } from '../../../core/http/erori';
import { TipFunctie } from '../../../shared/enums';
import { etichetaTipFunctie } from '../../../shared/etichete';
import { MandateFunctieService } from '../mandate-functie.service';

export interface DateInchideMandatDialog {
  mandatId: number;
  tipFunctie: TipFunctie;
  numeSubiect: string;
  dataInceput: string;
}

@Component({
  selector: 'app-inchide-mandat-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule
  ],
  templateUrl: './inchide-mandat-dialog.html',
  styleUrl: './inchide-mandat-dialog.scss'
})
export class InchideMandatDialog {
  private readonly api = inject(MandateFunctieService);
  private readonly dialogRef = inject(MatDialogRef<InchideMandatDialog>);
  readonly date = inject<DateInchideMandatDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly etichetaTipFunctie = etichetaTipFunctie;

  readonly form = this.fb.nonNullable.group({
    dataSfarsit: [this.dataAzi(), Validators.required]
  });

  async salveaza(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    if (v.dataSfarsit < this.date.dataInceput) {
      this.eroare.set('Data sfârșit nu poate fi anterioară datei de început a mandatului.');
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);
    try {
      await this.api.inchide(this.date.mandatId, { dataSfarsit: v.dataSfarsit });
      this.dialogRef.close(true);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }

  private dataAzi(): string {
    return new Date().toISOString().substring(0, 10);
  }
}