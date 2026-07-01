import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { extrageMesajEroare } from '../../../core/http/erori';
import { TipDispozitie } from '../../../shared/enums';
import { etichetaTipDispozitie } from '../../../shared/etichete';
import { Consilier } from '../../consilieri/consilieri.models';
import { ConsilieriService } from '../../consilieri/consilieri.service';
import { DispozitieDetalii } from '../dispozitii.models';
import { DispozitiiService } from '../dispozitii.service';

@Component({
  selector: 'app-creare-dispozitie-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatButtonModule
  ],
  templateUrl: './creare-dispozitie-dialog.html',
  styleUrl: './creare-dispozitie-dialog.scss'
})
export class CreareDispozitieDialog {
  private readonly api = inject(DispozitiiService);
  private readonly consilieriApi = inject(ConsilieriService);
  private readonly dialogRef = inject(MatDialogRef<CreareDispozitieDialog, DispozitieDetalii>);
  private readonly fb = inject(FormBuilder);

  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly consilieri = signal<Consilier[]>([]);

  readonly etichetaTip = etichetaTipDispozitie;
  readonly tipuri: TipDispozitie[] = [TipDispozitie.Normativ, TipDispozitie.Individual];

  readonly form = this.fb.nonNullable.group({
    tipDispozitie: [TipDispozitie.Normativ, Validators.required],
    titlu: ['', [Validators.required, Validators.maxLength(500)]],
    dataEmitere: [this.dataAzi(), Validators.required],
    emitentConsilierId: [null as number | null]
  });

  constructor() {
    this.incarcaConsilieri();
  }

  private async incarcaConsilieri(): Promise<void> {
    try {
      this.consilieri.set(await this.consilieriApi.lista());
    } catch {
      // Selectorul de emitent e opțional — dacă lista nu se încarcă, rămâne gol
      // (calea implicită = primarul derivat de backend din data emiterii).
      this.consilieri.set([]);
    }
  }

  async creeaza(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);
    const v = this.form.getRawValue();
    try {
      const rezultat = await this.api.creeaza({
        tipDispozitie: v.tipDispozitie,
        titlu: v.titlu.trim(),
        dataEmitere: v.dataEmitere,
        emitentConsilierId: v.emitentConsilierId
      });
      this.dialogRef.close(rezultat);
    } catch (err) {
      // ◆ Pe ORICE 400 (primar sau secretar lipsă la dată) → mesaj brut de la backend inline,
      // fără string-matching. Mesajul îi spune utilizatorului ce să facă (emitent înlocuitor / Funcții oficiale).
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }

  private dataAzi(): string {
    return new Date().toISOString().substring(0, 10);
  }
}
