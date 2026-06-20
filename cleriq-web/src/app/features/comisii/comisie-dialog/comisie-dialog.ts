import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { extrageMesajEroare } from '../../../core/http/erori';
import { Comisie } from '../comisii.models';
import { ComisiiService } from '../comisii.service';

export interface DateComisieDialog {
  comisie?: Comisie;
}

@Component({
  selector: 'app-comisie-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule
  ],
  templateUrl: './comisie-dialog.html',
  styleUrl: './comisie-dialog.scss'
})
export class ComisieDialog {
  private readonly api = inject(ComisiiService);
  private readonly dialogRef = inject(MatDialogRef<ComisieDialog>);
  private readonly date = inject<DateComisieDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly esteEditare = !!this.date.comisie;
  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    denumire: [this.date.comisie?.denumire ?? '', Validators.required],
    descriere: [this.date.comisie?.descriere ?? '']
  });

  async salveaza(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);

    const v = this.form.getRawValue();
    const denumire = v.denumire.trim();
    const descriere = v.descriere.trim() || null;

    try {
      const rezultat = this.esteEditare
        ? await this.api.actualizeaza(this.date.comisie!.id, { denumire, descriere })
        : await this.api.creeaza({ denumire, descriere });
      this.dialogRef.close(rezultat);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }
}