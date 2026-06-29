import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FUS_INSTITUTIE } from '../../../core/config';
import { extrageMesajEroare } from '../../../core/http/erori';
import { inputLocalLaUtcIso } from '../../../shared/data';
import { HclDetalii } from '../hcl.models';
import { HclService } from '../hcl.service';

export interface DatePublicareMolDialog {
  hclId: number;
}

@Component({
  selector: 'app-publicare-mol-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatProgressSpinnerModule
  ],
  templateUrl: './publicare-mol-dialog.html',
  styleUrl: './publicare-mol-dialog.scss'
})
export class PublicareMolDialog {
  private readonly api = inject(HclService);
  private readonly dialogRef = inject(MatDialogRef<PublicareMolDialog, HclDetalii>);
  private readonly date = inject<DatePublicareMolDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly FUS_INSTITUTIE = FUS_INSTITUTIE;

  readonly form = this.fb.nonNullable.group({
    data: [this.dataAzi(), Validators.required]
  });

  async publica(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);
    try {
      // Data publicării = miezul nopții în fusul instituției → ISO UTC pentru API (DateTime).
      const iso = inputLocalLaUtcIso(`${this.form.controls.data.value}T00:00`);
      const rezultat = await this.api.publicaMol(this.date.hclId, iso);
      this.dialogRef.close(rezultat);
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
