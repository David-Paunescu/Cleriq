import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { extrageMesajEroare } from '../../../core/http/erori';
import { formateazaDataDoar } from '../../../shared/data';
import { RaspunsPrefect } from '../../../shared/enums';
import { etichetaCanalTransmiterePrefect, etichetaRaspunsPrefect } from '../../../shared/etichete';
import { ComunicareDispozitiePrefect } from '../dispozitii.models';
import { DispozitiiService } from '../dispozitii.service';

export interface DateRaspunsPrefectDispozitieDialog {
  dispozitieId: number;
  comunicare: ComunicareDispozitiePrefect;
}

@Component({
  selector: 'app-raspuns-prefect-dispozitie-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatButtonModule
  ],
  templateUrl: './raspuns-prefect-dialog.html',
  styleUrl: './raspuns-prefect-dialog.scss'
})
export class RaspunsPrefectDispozitieDialog {
  private readonly api = inject(DispozitiiService);
  private readonly dialogRef = inject(MatDialogRef<RaspunsPrefectDispozitieDialog, ComunicareDispozitiePrefect>);
  private readonly date = inject<DateRaspunsPrefectDispozitieDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly comunicare = this.date.comunicare;
  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);

  readonly etichetaCanal = etichetaCanalTransmiterePrefect;
  readonly etichetaRaspuns = etichetaRaspunsPrefect;
  readonly formateazaDataDoar = formateazaDataDoar;
  readonly raspunsuri: RaspunsPrefect[] = [
    RaspunsPrefect.Acceptat, RaspunsPrefect.RespinsLegalitate,
    RaspunsPrefect.CereClarificari, RaspunsPrefect.FaraRaspuns
  ];

  readonly form = this.fb.group({
    raspuns: this.fb.control<RaspunsPrefect | null>(this.date.comunicare.raspunsPrefect),
    dataRaspuns: this.fb.nonNullable.control(this.date.comunicare.dataRaspunsPrefect ?? ''),
    obiectiiMotivate: this.fb.nonNullable.control(this.date.comunicare.obiectiiMotivate ?? ''),
    nrInregistrarePrefect: this.fb.nonNullable.control(this.date.comunicare.nrInregistrarePrefect ?? ''),
    dataConfirmarePrefect: this.fb.nonNullable.control(this.date.comunicare.dataConfirmarePrefect ?? ''),
    observatiiInterne: this.fb.nonNullable.control(this.date.comunicare.observatiiInterne ?? '')
  });

  async salveaza(): Promise<void> {
    this.seSalveaza.set(true);
    this.eroare.set(null);
    const v = this.form.getRawValue();
    try {
      const rezultat = await this.api.actualizeazaComunicare(
        this.date.dispozitieId, this.date.comunicare.id, {
          raspuns: v.raspuns ?? null,
          dataRaspuns: v.dataRaspuns.trim() || null,
          obiectiiMotivate: v.obiectiiMotivate.trim() || null,
          observatiiInterne: v.observatiiInterne.trim() || null,
          nrInregistrarePrefect: v.nrInregistrarePrefect.trim() || null,
          dataConfirmarePrefect: v.dataConfirmarePrefect.trim() || null
        });
      this.dialogRef.close(rezultat);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }
}
