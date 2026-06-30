import { Component, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { extrageMesajEroare } from '../../../core/http/erori';
import { MotivInvalidare } from '../../../shared/enums';
import { etichetaMotivInvalidare, etichetaTipRelatieHcl } from '../../../shared/etichete';
import { HclDetalii, RelatieHcl, RelatiiActiveInvalidare } from '../hcl.models';
import { HclService } from '../hcl.service';

export interface DateInvalidareDialog {
  hclId: number;
}

@Component({
  selector: 'app-invalidare-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatProgressSpinnerModule, MatIconModule
  ],
  templateUrl: './invalidare-dialog.html',
  styleUrl: './invalidare-dialog.scss'
})
export class InvalidareDialog {
  private readonly api = inject(HclService);
  private readonly dialogRef = inject(MatDialogRef<InvalidareDialog, HclDetalii>);
  private readonly date = inject<DateInvalidareDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly relatiiActive = signal<RelatiiActiveInvalidare | null>(null);

  readonly etichetaMotivInvalidare = etichetaMotivInvalidare;
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
    // Doar eroarea generică se resetează la editare; avertismentul de relații rămâne
    // (e despre legăturile actului, nu despre motiv) până la confirmarea explicită.
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

  numarRelatii(): number {
    const r = this.relatiiActive();
    return r ? r.relatiiSursaActive.length + r.relatiiTintaActive.length : 0;
  }

  // Acest HCL e sursa relației → arătăm acțiunea + ținta.
  textSursa(r: RelatieHcl): string {
    const tinta = r.numarTintaFormatat ?? r.referintaActExternText ?? r.titluTinta ?? 'act extern';
    return `${etichetaTipRelatieHcl(r.tipRelatie)} → ${tinta}`;
  }

  // Acest HCL e ținta relației → arătăm sursa + acțiunea.
  textTinta(r: RelatieHcl): string {
    const sursa = r.numarSursaFormatat ?? r.titluSursa;
    return `${sursa}: ${etichetaTipRelatieHcl(r.tipRelatie)} acest act`;
  }

  async invalideaza(confirmaCuRelatiiActive = false): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);
    const v = this.form.getRawValue();
    try {
      const rezultat = await this.api.invalideaza(this.date.hclId, {
        motiv: v.motiv!,
        motivAltulText: v.motiv === MotivInvalidare.Altul ? v.motivAltulText.trim() : null,
        refInvalidare: v.refInvalidare.trim() || null,
        confirmaCuRelatiiActive
      });
      this.dialogRef.close(rezultat);
    } catch (err) {
      this.trateazaEroare(err);
    } finally {
      this.seSalveaza.set(false);
    }
  }

  private trateazaEroare(err: unknown): void {
    if (err instanceof HttpErrorResponse && err.status === 409
        && err.error && typeof err.error === 'object' && 'relatiiSursaActive' in err.error) {
      this.relatiiActive.set(err.error as RelatiiActiveInvalidare);
      return;
    }
    this.eroare.set(extrageMesajEroare(err));
  }
}
