import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { extrageMesajEroare } from '../../../core/http/erori';
import { etichetaTipFunctie } from '../../../shared/etichete';
import { MandatFunctie } from '../mandate-functie.models';
import { MandateFunctieService } from '../mandate-functie.service';

export interface DateEditeazaMandatDialog {
  mandat: MandatFunctie;
}

@Component({
  selector: 'app-editeaza-mandat-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule
  ],
  templateUrl: './editeaza-mandat-dialog.html',
  styleUrl: './editeaza-mandat-dialog.scss'
})
export class EditeazaMandatDialog {
  private readonly api = inject(MandateFunctieService);
  private readonly dialogRef = inject(MatDialogRef<EditeazaMandatDialog>);
  readonly date = inject<DateEditeazaMandatDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly etichetaTipFunctie = etichetaTipFunctie;
  readonly numeSubiect = this.date.mandat.numeCompletPersoana
    ?? this.date.mandat.numeCompletConsilier ?? '—';

  readonly form = this.fb.nonNullable.group({
    dataInceput: [this.date.mandat.dataInceput, Validators.required],
    dataSfarsit: [this.date.mandat.dataSfarsit as string | null],
    nrActNumire: [this.date.mandat.nrActNumire ?? '']
  });

  async salveaza(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    if (v.dataSfarsit && v.dataSfarsit < v.dataInceput) {
      this.eroare.set('Data sfârșit nu poate fi anterioară datei de început.');
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);
    try {
      await this.api.actualizeaza(this.date.mandat.id, {
        dataInceput: v.dataInceput,
        dataSfarsit: v.dataSfarsit || null,
        nrActNumire: v.nrActNumire.trim() || null
      });
      this.dialogRef.close(true);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }
}