import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { extrageMesajEroare } from '../../../core/http/erori';
import { MotivInvalidare } from '../../../shared/enums';
import { etichetaMotivInvalidareDispozitie } from '../../../shared/etichete';
import { DispozitieDetalii } from '../dispozitii.models';
import { DispozitiiService } from '../dispozitii.service';

export interface DateInvalidareDispozitieDialog {
  dispozitieId: number;
}

@Component({
  selector: 'app-invalidare-dispozitie-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatProgressSpinnerModule
  ],
  templateUrl: './invalidare-dialog.html',
  styleUrl: './invalidare-dialog.scss'
})
export class InvalidareDispozitieDialog {
  private readonly api = inject(DispozitiiService);
  private readonly dialogRef = inject(MatDialogRef<InvalidareDispozitieDialog, DispozitieDetalii>);
  private readonly date = inject<DateInvalidareDispozitieDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);

  readonly etichetaMotivInvalidareDispozitie = etichetaMotivInvalidareDispozitie;
  readonly motive: MotivInvalidare[] = [
    MotivInvalidare.AnulatInstanta, MotivInvalidare.AbrogatHclUlterior,
    MotivInvalidare.Retractat, MotivInvalidare.Caduc,
    MotivInvalidare.Inexistent, MotivInvalidare.Altul
  ];

  readonly form = this.fb.nonNullable.group({
    motiv: [null as MotivInvalidare | null, Validators.required],
    motivAltulText: ['', Validators.maxLength(300)],
    refInvalidare: ['']
  });

  private readonly motivSelectat = toSignal(
    this.form.controls.motiv.valueChanges,
    { initialValue: this.form.controls.motiv.value });
  readonly esteAltul = computed(() => this.motivSelectat() === MotivInvalidare.Altul);

  constructor() {
    // Eroarea generică se resetează la editare.
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      if (this.eroare()) this.eroare.set(null);
    });
    // „Altul" → text liber obligatoriu (oglindă backend); altfel se curăță.
    this.form.controls.motiv.valueChanges.pipe(takeUntilDestroyed()).subscribe(motiv => {
      const ctrl = this.form.controls.motivAltulText;
      if (motiv === MotivInvalidare.Altul) {
        ctrl.addValidators(Validators.required);
      } else {
        ctrl.removeValidators(Validators.required);
        ctrl.setValue('');
      }
      ctrl.updateValueAndValidity({ emitEvent: false });
    });
  }

  async invalideaza(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);
    const v = this.form.getRawValue();
    try {
      const rezultat = await this.api.invalideaza(this.date.dispozitieId, {
        motiv: v.motiv!,
        motivAltulText: v.motiv === MotivInvalidare.Altul ? v.motivAltulText.trim() : null,
        refInvalidare: v.refInvalidare.trim() || null
      });
      this.dialogRef.close(rezultat);
    } catch (err) {
      // ◆ Fără relații pe dispoziție (Faza 7). 409-ul de revocare-in-circuit (Individual) =
      // Conflict("string") simplu → mesaj generic inline, fără branch structurat.
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }
}
