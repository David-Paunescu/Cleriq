import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { extrageMesajEroare } from '../../../core/http/erori';
import { Consilier } from '../../consilieri/consilieri.models';
import { ConsilieriService } from '../../consilieri/consilieri.service';
import { RolComisie } from '../../../shared/enums';
import { etichetaRolComisie } from '../../../shared/etichete';
import { ComisiiService } from '../comisii.service';

export interface DateMembruDialog {
  comisieId: number;
  consilieriDejaActivi: number[];
}

@Component({
  selector: 'app-membru-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatButtonModule, MatProgressSpinnerModule
  ],
  templateUrl: './membru-dialog.html',
  styleUrl: './membru-dialog.scss'
})
export class MembruDialog {
  private readonly api = inject(ComisiiService);
  private readonly consilieriApi = inject(ConsilieriService);
  private readonly dialogRef = inject(MatDialogRef<MembruDialog>);
  private readonly date = inject<DateMembruDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seIncarcaConsilieri = signal(true);
  readonly consilieri = signal<Consilier[]>([]);
  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);

  readonly roluri: RolComisie[] = [
    RolComisie.Presedinte, RolComisie.Secretar, RolComisie.Membru
  ];

  readonly etichetaRol = etichetaRolComisie;

  readonly consilieriDisponibili = computed(() =>
    this.consilieri()
      .filter(c => c.activ && !this.date.consilieriDejaActivi.includes(c.id))
      .sort((a, b) => a.numeComplet.localeCompare(b.numeComplet)));

  readonly form = this.fb.nonNullable.group({
    consilierId: [null as number | null, Validators.required],
    rol: [RolComisie.Membru, Validators.required],
    dataInceput: [this.dataAzi(), Validators.required]
  });

  constructor() {
    this.incarcaConsilieri();
  }

  async incarcaConsilieri(): Promise<void> {
    try {
      this.consilieri.set(await this.consilieriApi.lista());
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarcaConsilieri.set(false);
    }
  }

  async salveaza(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);

    const v = this.form.getRawValue();

    try {
      await this.api.adaugaMembru(this.date.comisieId, {
        consilierId: v.consilierId!,
        rol: v.rol,
        dataInceput: v.dataInceput
      });
      this.dialogRef.close(true);
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