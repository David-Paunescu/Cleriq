import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { extrageMesajEroare } from '../../../core/http/erori';
import { ComisiiService } from '../comisii.service';

export interface DateCorectareDataDialog {
  comisieId: number;
  consilierId: number;
  numeComplet: string;
  dataInceputCurenta: string;
  dataSfarsit: string | null;
}

@Component({
  selector: 'app-corectare-data-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule
  ],
  templateUrl: './corectare-data-dialog.html',
  styleUrl: './corectare-data-dialog.scss'
})
export class CorectareDataDialog {
  private readonly api = inject(ComisiiService);
  private readonly dialogRef = inject(MatDialogRef<CorectareDataDialog>);
  readonly date = inject<DateCorectareDataDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    dataInceput: [this.date.dataInceputCurenta, Validators.required]
  });

  async salveaza(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    if (this.date.dataSfarsit && v.dataInceput > this.date.dataSfarsit) {
      this.eroare.set('Data început nu poate fi după data sfârșit.');
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);

    try {
      await this.api.actualizeazaDataInceputMembru(
        this.date.comisieId, this.date.consilierId,
        { dataInceput: v.dataInceput });
      this.dialogRef.close(true);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }
}