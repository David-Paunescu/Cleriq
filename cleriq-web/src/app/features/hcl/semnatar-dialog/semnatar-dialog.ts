import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { extrageMesajEroare } from '../../../core/http/erori';
import { RolSemnatar } from '../../../shared/enums';
import { etichetaRolSemnatar } from '../../../shared/etichete';
import { Consilier } from '../../consilieri/consilieri.models';
import { ConsilieriService } from '../../consilieri/consilieri.service';
import { Persoana } from '../../persoane/persoane.models';
import { PersoaneService } from '../../persoane/persoane.service';
import { HclDetalii } from '../hcl.models';
import { HclService } from '../hcl.service';

export interface DateSemnatarDialog {
  hclId: number;
  ordineAfisareSugerata: number;
}

@Component({
  selector: 'app-semnatar-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatRadioModule, MatButtonModule, MatProgressSpinnerModule
  ],
  templateUrl: './semnatar-dialog.html',
  styleUrl: './semnatar-dialog.scss'
})
export class SemnatarDialog {
  private readonly api = inject(HclService);
  private readonly consilieriApi = inject(ConsilieriService);
  private readonly persoaneApi = inject(PersoaneService);
  private readonly dialogRef = inject(MatDialogRef<SemnatarDialog, HclDetalii>);
  private readonly date = inject<DateSemnatarDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seIncarcaInitial = signal(true);
  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly persoane = signal<Persoana[]>([]);
  readonly consilieri = signal<Consilier[]>([]);

  readonly etichetaRolSemnatar = etichetaRolSemnatar;
  readonly roluri: RolSemnatar[] = [
    RolSemnatar.PresedinteSedinta, RolSemnatar.SecretarUat, RolSemnatar.SemnatarAlternativArt140
  ];

  readonly form = this.fb.nonNullable.group({
    rol: [RolSemnatar.SemnatarAlternativArt140, Validators.required],
    persoanaId: [null as number | null],
    consilierId: [null as number | null],
    ordineAfisare: [this.date.ordineAfisareSugerata, [Validators.required, Validators.min(1)]]
  });

  readonly rolCurent = toSignal(this.form.controls.rol.valueChanges,
    { initialValue: this.form.controls.rol.value });

  readonly necesitaPersoana = computed(() => this.rolCurent() === RolSemnatar.SecretarUat);
  readonly necesitaConsilier = computed(() =>
    this.rolCurent() === RolSemnatar.PresedinteSedinta
    || this.rolCurent() === RolSemnatar.SemnatarAlternativArt140);
  readonly esteAlternativ = computed(() =>
    this.rolCurent() === RolSemnatar.SemnatarAlternativArt140);

  constructor() {
    this.incarcaInitial();
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      if (this.eroare()) this.eroare.set(null);
    });
  }

  async adauga(): Promise<void> {
    const v = this.form.getRawValue();
    if (this.necesitaPersoana() && v.persoanaId === null) {
      this.eroare.set('Selectează o persoană (Secretar UAT).');
      return;
    }
    if (this.necesitaConsilier() && v.consilierId === null) {
      this.eroare.set('Selectează un consilier.');
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);
    try {
      const rezultat = await this.api.adaugaSemnatar(this.date.hclId, {
        rol: v.rol,
        persoanaId: this.necesitaPersoana() ? v.persoanaId : null,
        consilierId: this.necesitaConsilier() ? v.consilierId : null,
        ordineAfisare: v.ordineAfisare
      });
      this.dialogRef.close(rezultat);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }

  private async incarcaInitial(): Promise<void> {
    try {
      const [persoane, consilieri] = await Promise.all([
        this.persoaneApi.lista(), this.consilieriApi.lista()
      ]);
      this.persoane.set(persoane);
      this.consilieri.set(consilieri.filter(c => c.activ));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarcaInitial.set(false);
    }
  }
}
