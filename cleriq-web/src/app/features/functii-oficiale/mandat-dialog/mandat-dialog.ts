import { Component, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { extrageMesajEroare } from '../../../core/http/erori';
import { TipFunctie } from '../../../shared/enums';
import { etichetaTipFunctie } from '../../../shared/etichete';
import { Persoana } from '../../persoane/persoane.models';
import { PersoaneService } from '../../persoane/persoane.service';
import { SubiectIstoric } from '../functii-istorice.models';
import { FunctiiIstoriceService } from '../functii-istorice.service';
import { MandateFunctieService } from '../mandate-functie.service';

export interface DateMandatDialog {
  tipPreselectat: TipFunctie;
  existaMandatPrimarActiv: boolean;
  existaSecretarUatActiv: boolean;
  consilieriDejaViceprimari: number[];
}

@Component({
  selector: 'app-mandat-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatRadioModule, MatButtonModule,
    MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './mandat-dialog.html',
  styleUrl: './mandat-dialog.scss'
})
export class MandatDialog {
  private readonly api = inject(MandateFunctieService);
  private readonly persoaneApi = inject(PersoaneService);
  private readonly functiiIstorice = inject(FunctiiIstoriceService);
  private readonly dialogRef = inject(MatDialogRef<MandatDialog>);
  readonly date = inject<DateMandatDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seIncarcaInitial = signal(true);
  readonly seIncarcaConsilieri = signal(
    this.date.tipPreselectat === TipFunctie.Viceprimar);
  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly persoane = signal<Persoana[]>([]);
  readonly consilieriValizi = signal<SubiectIstoric[]>([]);

  readonly TipFunctie = TipFunctie;
  readonly etichetaTipFunctie = etichetaTipFunctie;
  readonly tipuri: TipFunctie[] = [
    TipFunctie.Primar, TipFunctie.Viceprimar, TipFunctie.SecretarUat
  ];

  readonly form = this.fb.nonNullable.group({
    tipFunctie: [this.date.tipPreselectat, Validators.required],
    persoanaId: [null as number | null],
    consilierId: [null as number | null],
    dataInceput: this.fb.nonNullable.control(this.dataAzi(), {
      validators: [Validators.required],
      updateOn: 'blur'
    }),
    dataSfarsit: [null as string | null],
    nrActNumire: ['']
  });

  readonly tipFunctieCurent = toSignal(
    this.form.controls.tipFunctie.valueChanges,
    { initialValue: this.form.controls.tipFunctie.value });

  readonly dataInceputCurenta = toSignal(
    this.form.controls.dataInceput.valueChanges,
    { initialValue: this.form.controls.dataInceput.value });

  readonly necesitaPersoana = computed(() =>
    this.tipFunctieCurent() === TipFunctie.Primar
    || this.tipFunctieCurent() === TipFunctie.SecretarUat);

  readonly necesitaConsilier = computed(() =>
    this.tipFunctieCurent() === TipFunctie.Viceprimar);

  readonly consilieriDisponibili = computed(() =>
    this.consilieriValizi().filter(c =>
      !this.date.consilieriDejaViceprimari.includes(c.id)));

  constructor() {
    this.incarcaInitial();

    this.form.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        if (this.eroare()) this.eroare.set(null);
      });

    effect(() => {
      const data = this.dataInceputCurenta();
      if (this.necesitaConsilier() && data) {
        this.refetchConsilieri(data);
      }
    });

    effect(() => {
      if (this.seIncarcaConsilieri()) {
        this.form.controls.consilierId.disable({ emitEvent: false });
      } else {
        this.form.controls.consilierId.enable({ emitEvent: false });
      }
    });
  }

  esteTipDezactivat(tip: TipFunctie): boolean {
    if (tip === TipFunctie.Primar) return this.date.existaMandatPrimarActiv;
    if (tip === TipFunctie.SecretarUat) return this.date.existaSecretarUatActiv;
    return false;
  }

  tooltipPentruTipDezactivat(tip: TipFunctie): string {
    if (tip === TipFunctie.Primar && this.date.existaMandatPrimarActiv) {
      return 'Există deja un mandat activ de Primar. Închide-l pentru a adăuga altul.';
    }
    if (tip === TipFunctie.SecretarUat && this.date.existaSecretarUatActiv) {
      return 'Există deja un mandat activ de Secretar UAT. Închide-l pentru a adăuga altul.';
    }
    return '';
  }

  async salveaza(): Promise<void> {
    (document.activeElement as HTMLElement | null)?.blur();

    const v = this.form.getRawValue();

    if (this.necesitaPersoana() && v.persoanaId === null) {
      this.eroare.set('Selectează o persoană.');
      return;
    }
    if (this.necesitaConsilier() && v.consilierId === null) {
      this.eroare.set('Selectează un consilier.');
      return;
    }
    if (!v.dataInceput) {
      this.eroare.set('Data început este obligatorie.');
      return;
    }
    if (v.dataSfarsit && v.dataSfarsit < v.dataInceput) {
      this.eroare.set('Data sfârșit nu poate fi anterioară datei de început.');
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);
    try {
      const rezultat = await this.api.creeaza({
        tipFunctie: v.tipFunctie,
        persoanaId: this.necesitaPersoana() ? v.persoanaId : null,
        consilierId: this.necesitaConsilier() ? v.consilierId : null,
        dataInceput: v.dataInceput,
        dataSfarsit: v.dataSfarsit || null,
        nrActNumire: v.nrActNumire.trim() || null
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
      const persoane = await this.persoaneApi.lista();
      this.persoane.set(persoane);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarcaInitial.set(false);
    }
  }

  private async refetchConsilieri(data: string): Promise<void> {
    this.seIncarcaConsilieri.set(true);
    try {
      const lista = await this.functiiIstorice.consilieri(data);
      this.consilieriValizi.set(lista);

      const selectat = this.form.controls.consilierId.value;
      if (selectat !== null && !lista.some(c => c.id === selectat)) {
        this.form.controls.consilierId.setValue(null);
      }
    } catch (err) {
      if (!this.eroare()) {
        this.eroare.set(extrageMesajEroare(err));
      }
    } finally {
      this.seIncarcaConsilieri.set(false);
    }
  }

  private dataAzi(): string {
    return new Date().toISOString().substring(0, 10);
  }
}