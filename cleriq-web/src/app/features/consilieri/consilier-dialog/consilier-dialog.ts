import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { extrageMesajEroare } from '../../../core/http/erori';
import { Consilier } from '../consilieri.models';
import { ConsilieriService } from '../consilieri.service';

export interface DateConsilierDialog {
  consilier?: Consilier;
}

@Component({
  selector: 'app-consilier-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatSlideToggleModule
  ],
  templateUrl: './consilier-dialog.html',
  styleUrl: './consilier-dialog.scss'
})
export class ConsilierDialog {
  private readonly api = inject(ConsilieriService);
  private readonly dialogRef = inject(MatDialogRef<ConsilierDialog>);
  private readonly date = inject<DateConsilierDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly esteEditare = !!this.date.consilier;
  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    numeComplet: [this.date.consilier?.numeComplet ?? '', Validators.required],
    email: [this.date.consilier?.email ?? '', Validators.email],
    telefon: [this.date.consilier?.telefon ?? ''],
    activ: [this.date.consilier?.activ ?? true]
  });

  async salveaza(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);

    const v = this.form.getRawValue();
    const numeComplet = v.numeComplet.trim();
    const email = v.email.trim() || null;
    const telefon = v.telefon.trim() || null;

    try {
      const rezultat = this.esteEditare
        ? await this.api.actualizeaza(this.date.consilier!.id,
            { numeComplet, email, telefon, activ: v.activ })
        : await this.api.creeaza({ numeComplet, email, telefon });
      this.dialogRef.close(rezultat);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }
}