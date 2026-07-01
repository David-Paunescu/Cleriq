import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { extrageMesajEroare } from '../../../core/http/erori';
import { AVERTISMENT_PUBLICARE_INDIVIDUALA } from '../../../shared/text';
import { DispozitieDetalii } from '../dispozitii.models';
import { DispozitiiService } from '../dispozitii.service';

// Deschis DOAR pentru Individual — Normativul se publică pe portal prin toggle direct (fără dialog).
export interface DatePublicarePortalDispozitieDialog {
  dispozitieId: number;
}

@Component({
  selector: 'app-publicare-portal-dispozitie-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatProgressSpinnerModule, MatIconModule
  ],
  templateUrl: './publicare-portal-dialog.html',
  styleUrl: './publicare-portal-dialog.scss'
})
export class PublicarePortalDispozitieDialog {
  private readonly api = inject(DispozitiiService);
  private readonly dialogRef = inject(MatDialogRef<PublicarePortalDispozitieDialog, DispozitieDetalii>);
  private readonly date = inject<DatePublicarePortalDispozitieDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly avertismentIndividual = AVERTISMENT_PUBLICARE_INDIVIDUALA;

  readonly form = this.fb.nonNullable.group({
    motiv: ['', [Validators.required, Validators.maxLength(1000)]]
  });

  async publica(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.seSalveaza.set(true);
    this.eroare.set(null);
    try {
      const rezultat = await this.api.publica(
        this.date.dispozitieId, true, true, this.form.controls.motiv.value.trim());
      this.dialogRef.close(rezultat);
    } catch (err) {
      // Plasă defensivă: soft-409 { mesaj, necesitaConfirmarePublicareIndividuala } → mesaj inline.
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }
}
