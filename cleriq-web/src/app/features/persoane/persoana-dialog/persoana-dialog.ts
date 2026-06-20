import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { extrageMesajEroare } from '../../../core/http/erori';
import { Persoana } from '../persoane.models';
import { PersoaneService } from '../persoane.service';

export interface DatePersoanaDialog {
  persoana?: Persoana;
}

@Component({
  selector: 'app-persoana-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule
  ],
  templateUrl: './persoana-dialog.html',
  styleUrl: './persoana-dialog.scss'
})
export class PersoanaDialog {
  private readonly api = inject(PersoaneService);
  private readonly dialogRef = inject(MatDialogRef<PersoanaDialog>);
  private readonly date = inject<DatePersoanaDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly esteEditare = !!this.date.persoana;
  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    numeComplet: [this.date.persoana?.numeComplet ?? '', Validators.required],
    email: [this.date.persoana?.email ?? '', Validators.email],
    telefon: [this.date.persoana?.telefon ?? '']
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
        ? await this.api.actualizeaza(this.date.persoana!.id, { numeComplet, email, telefon })
        : await this.api.creeaza({ numeComplet, email, telefon });
      this.dialogRef.close(rezultat);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }
}