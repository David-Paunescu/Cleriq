import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { extrageMesajEroare } from '../../../core/http/erori';
import { ComisiiService } from '../comisii.service';

export interface DateInchideMembruDialog {
  comisieId: number;
  consilierId: number;
  numeComplet: string;
  dataInceput: string;
}

@Component({
  selector: 'app-inchide-membru-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule
  ],
  templateUrl: './inchide-membru-dialog.html',
  styleUrl: './inchide-membru-dialog.scss'
})
export class InchideMembruDialog {
  private readonly api = inject(ComisiiService);
  private readonly dialogRef = inject(MatDialogRef<InchideMembruDialog>);
  readonly date = inject<DateInchideMembruDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);

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
      this.eroare.set('Data sfârșit nu poate fi anterioară datei de început a membriei.');
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);

    try {
      await this.api.scoateMembru(
        this.date.comisieId, this.date.consilierId, v.dataSfarsit);
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