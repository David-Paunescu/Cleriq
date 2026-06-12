import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { extrageMesajEroare } from '../../../core/http/erori';
import { Consilier } from '../consilieri.models';
import { ConsilieriService } from '../consilieri.service';

export interface DateContDialog {
  consilier: Consilier;
}

@Component({
  selector: 'app-cont-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule
  ],
  templateUrl: './cont-dialog.html',
  styleUrl: './cont-dialog.scss'
})
export class ContDialog {
  private readonly api = inject(ConsilieriService);
  private readonly dialogRef = inject(MatDialogRef<ContDialog>);
  private readonly fb = inject(FormBuilder);

  readonly date = inject<DateContDialog>(MAT_DIALOG_DATA);
  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    parola: ['', [Validators.required, Validators.minLength(8)]]
  });

  async creeaza(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);

    try {
      await this.api.creeazaCont(this.date.consilier.id, this.form.getRawValue());
      this.dialogRef.close(true);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }
}