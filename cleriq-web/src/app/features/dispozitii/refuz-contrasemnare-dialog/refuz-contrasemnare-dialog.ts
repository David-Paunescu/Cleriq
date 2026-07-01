import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { extrageMesajEroare } from '../../../core/http/erori';
import { DispozitieDetalii } from '../dispozitii.models';
import { DispozitiiService } from '../dispozitii.service';

export interface DateRefuzContrasemnareDialog {
  dispozitieId: number;
}

@Component({
  selector: 'app-refuz-contrasemnare-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule
  ],
  templateUrl: './refuz-contrasemnare-dialog.html',
  styleUrl: './refuz-contrasemnare-dialog.scss'
})
export class RefuzContrasemnareDialog {
  private readonly api = inject(DispozitiiService);
  private readonly dialogRef = inject(MatDialogRef<RefuzContrasemnareDialog, DispozitieDetalii>);
  private readonly date = inject<DateRefuzContrasemnareDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    obiectie: ['', [Validators.required, Validators.maxLength(2000)]]
  });

  async refuza(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.seSalveaza.set(true);
    this.eroare.set(null);
    try {
      const rezultat = await this.api.refuzaContrasemnare(
        this.date.dispozitieId, this.form.controls.obiectie.value.trim());
      this.dialogRef.close(rezultat);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }
}
