import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { extrageMesajEroare } from '../../../core/http/erori';
import { HclDetalii } from '../hcl.models';
import { HclService } from '../hcl.service';

export interface DateAnulareMolDialog {
  hclId: number;
}

@Component({
  selector: 'app-anulare-mol-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatProgressSpinnerModule
  ],
  templateUrl: './anulare-mol-dialog.html',
  styleUrl: './anulare-mol-dialog.scss'
})
export class AnulareMolDialog {
  private readonly api = inject(HclService);
  private readonly dialogRef = inject(MatDialogRef<AnulareMolDialog, HclDetalii>);
  private readonly date = inject<DateAnulareMolDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    motiv: ['', [Validators.required, Validators.maxLength(1000)]]
  });

  async anuleaza(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.seSalveaza.set(true);
    this.eroare.set(null);
    try {
      const rezultat = await this.api.anuleazaMol(
        this.date.hclId, this.form.controls.motiv.value.trim());
      this.dialogRef.close(rezultat);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }
}
