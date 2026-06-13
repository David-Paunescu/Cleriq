import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FUS_INSTITUTIE } from '../../../core/config';
import { extrageMesajEroare } from '../../../core/http/erori';
import { inputLocalLaUtcIso, utcLaInputLocal } from '../../../shared/data';
import { ModDesfasurare, TipSedinta } from '../../../shared/enums';
import { etichetaModDesfasurare, etichetaTipSedinta } from '../../../shared/etichete';
import { actiuniPermise } from '../sedinte.permisiuni';
import { SedinteService } from '../sedinte.service';

@Component({
  selector: 'app-sedinta-form',
  imports: [
    ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './sedinta-form.html',
  styleUrl: './sedinta-form.scss'
})
export class SedintaForm {
  private readonly api = inject(SedinteService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  private readonly idDinRuta = this.route.snapshot.paramMap.get('id');
  readonly id = this.idDinRuta ? Number(this.idDinRuta) : null;
  readonly esteEditare = this.id !== null;

  readonly fusInstitutie = FUS_INSTITUTIE;
  readonly seIncarca = signal(false);
  readonly seSalveaza = signal(false);
  readonly eroareIncarcare = signal<string | null>(null);
  readonly eroareSalvare = signal<string | null>(null);

  readonly tipuri: TipSedinta[] = [
    TipSedinta.Ordinara, TipSedinta.Extraordinara, TipSedinta.DeIndata
  ];
  readonly moduri: ModDesfasurare[] = [
    ModDesfasurare.Fizic, ModDesfasurare.Online, ModDesfasurare.Hibrid
  ];

  readonly form = this.fb.nonNullable.group({
    titlu: ['', Validators.required],
    numar: [''],
    tip: [TipSedinta.Ordinara, Validators.required],
    dataOra: ['', Validators.required],
    loc: [''],
    modDesfasurare: [ModDesfasurare.Fizic, Validators.required]
  });

  readonly etichetaTip = etichetaTipSedinta;
  readonly etichetaMod = etichetaModDesfasurare;

  constructor() {
    if (this.esteEditare) this.incarcaPentruEditare();
  }

  private async incarcaPentruEditare(): Promise<void> {
    this.seIncarca.set(true);
    this.eroareIncarcare.set(null);
    try {
      const s = await this.api.detalii(this.id!);
      if (!actiuniPermise(s.status).poateEdita) {
        this.eroareIncarcare.set(
          'Această ședință nu mai poate fi editată (statusul nu permite).');
        return;
      }
      this.form.setValue({
        titlu: s.titlu,
        numar: s.numar ?? '',
        tip: s.tip,
        dataOra: utcLaInputLocal(s.dataOra),
        loc: s.loc ?? '',
        modDesfasurare: s.modDesfasurare
      });
    } catch (err) {
      this.eroareIncarcare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  async salveaza(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.seSalveaza.set(true);
    this.eroareSalvare.set(null);

    const v = this.form.getRawValue();
    const cerere = {
      titlu: v.titlu.trim(),
      numar: v.numar.trim() || null,
      tip: v.tip,
      dataOra: inputLocalLaUtcIso(v.dataOra),
      loc: v.loc.trim() || null,
      modDesfasurare: v.modDesfasurare
    };

    try {
      const rezultat = this.esteEditare
        ? await this.api.actualizeaza(this.id!, cerere)
        : await this.api.creeaza(cerere);
      this.snackBar.open(
        this.esteEditare ? 'Ședință actualizată.' : 'Ședință creată.',
        'Închide', { duration: 4000 });
      this.router.navigate(['/sedinte', rezultat.id]);
    } catch (err) {
      this.eroareSalvare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }

  renunta(): void {
    if (this.esteEditare) {
      this.router.navigate(['/sedinte', this.id]);
    } else {
      this.router.navigate(['/sedinte']);
    }
  }
}