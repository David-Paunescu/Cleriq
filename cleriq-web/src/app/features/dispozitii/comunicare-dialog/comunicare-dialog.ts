import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { extrageMesajEroare } from '../../../core/http/erori';
import { CanalTransmiterePrefect } from '../../../shared/enums';
import { etichetaCanalTransmiterePrefect } from '../../../shared/etichete';
import { ComunicareDispozitiePrefect } from '../dispozitii.models';
import { DispozitiiService } from '../dispozitii.service';

export interface DateComunicareDispozitieDialog {
  dispozitieId: number;
}

@Component({
  selector: 'app-comunicare-dispozitie-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatButtonModule
  ],
  templateUrl: './comunicare-dialog.html',
  styleUrl: './comunicare-dialog.scss'
})
export class ComunicareDispozitieDialog {
  private readonly api = inject(DispozitiiService);
  private readonly dialogRef = inject(MatDialogRef<ComunicareDispozitieDialog, ComunicareDispozitiePrefect>);
  private readonly date = inject<DateComunicareDispozitieDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);

  readonly etichetaCanal = etichetaCanalTransmiterePrefect;
  readonly canale: CanalTransmiterePrefect[] = [
    CanalTransmiterePrefect.Posta, CanalTransmiterePrefect.EmailOficial,
    CanalTransmiterePrefect.Curier, CanalTransmiterePrefect.Prezentare,
    CanalTransmiterePrefect.ePoartal, CanalTransmiterePrefect.Altul
  ];

  readonly form = this.fb.nonNullable.group({
    dataTrimiteri: [this.dataAzi(), Validators.required],
    canalTransmitere: [CanalTransmiterePrefect.EmailOficial, Validators.required],
    nrInregistrarePrefect: [''],
    observatiiInterne: ['']
  });

  async adauga(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);
    const v = this.form.getRawValue();
    try {
      const rezultat = await this.api.adaugaComunicare(this.date.dispozitieId, {
        dataTrimiteri: v.dataTrimiteri,
        canalTransmitere: v.canalTransmitere,
        nrInregistrarePrefect: v.nrInregistrarePrefect.trim() || null,
        dataConfirmarePrefect: null,
        observatiiInterne: v.observatiiInterne.trim() || null
      });
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
