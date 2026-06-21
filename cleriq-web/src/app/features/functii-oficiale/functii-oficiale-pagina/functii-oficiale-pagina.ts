import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/auth/auth.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { TipFunctie } from '../../../shared/enums';
import { FunctiiIstoriceService } from '../functii-istorice.service';
import { DateMandatDialog, MandatDialog } from '../mandat-dialog/mandat-dialog';
import { MandatFunctie } from '../mandate-functie.models';
import { MandateFunctieService } from '../mandate-functie.service';

interface ClasificareMandate {
  active: MandatFunctie[];
  istorice: MandatFunctie[];
}

function clasificaMandate(toate: MandatFunctie[], tip: TipFunctie): ClasificareMandate {
  const filtrate = toate.filter(m => m.tipFunctie === tip);
  const sortDesc = (a: MandatFunctie, b: MandatFunctie) =>
    b.dataInceput.localeCompare(a.dataInceput);
  return {
    active: filtrate.filter(m => m.dataSfarsit === null).sort(sortDesc),
    istorice: filtrate.filter(m => m.dataSfarsit !== null).sort(sortDesc)
  };
}

@Component({
  selector: 'app-functii-oficiale-pagina',
  imports: [
    NgTemplateOutlet, MatCardModule, MatIconModule, MatButtonModule,
    MatSlideToggleModule, MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './functii-oficiale-pagina.html',
  styleUrl: './functii-oficiale-pagina.scss'
})
export class FunctiiOficialePagina {
  private readonly api = inject(MandateFunctieService);
  private readonly functiiIstorice = inject(FunctiiIstoriceService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly mandate = signal<MandatFunctie[]>([]);
  readonly consilieriValiziAzi = signal<Set<number>>(new Set());
  readonly includeIstoric = signal(false);

  readonly TipFunctie = TipFunctie;
  readonly esteAdmin = computed(() => this.auth.areRol('Admin'));

  readonly mandatePrimar = computed(() =>
    clasificaMandate(this.mandate(), TipFunctie.Primar));
  readonly mandateViceprimar = computed(() =>
    clasificaMandate(this.mandate(), TipFunctie.Viceprimar));
  readonly mandateSecretarUat = computed(() =>
    clasificaMandate(this.mandate(), TipFunctie.SecretarUat));

  readonly existaMandatPrimarActiv = computed(() =>
    this.mandatePrimar().active.length > 0);
  readonly existaSecretarUatActiv = computed(() =>
    this.mandateSecretarUat().active.length > 0);

  readonly consilieriDejaViceprimari = computed(() =>
    this.mandateViceprimar().active
      .map(m => m.consilierId)
      .filter((id): id is number => id !== null));

  readonly fantomeIds = computed(() => {
    const activi = this.consilieriValiziAzi();
    const ids = new Set<number>();
    for (const m of this.mandateViceprimar().active) {
      if (m.consilierId !== null && !activi.has(m.consilierId)) {
        ids.add(m.id);
      }
    }
    return ids;
  });

  constructor() {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      const azi = this.dataAzi();
      const [mandate, viceprimariAzi] = await Promise.all([
        this.api.lista(),
        this.functiiIstorice.viceprimari(azi)
      ]);
      this.mandate.set(mandate);
      this.consilieriValiziAzi.set(new Set(viceprimariAzi.map(v => v.consilierId)));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  toggleIstoric(): void {
    this.includeIstoric.update(v => !v);
  }

  esteFantoma(mandatId: number): boolean {
    return this.fantomeIds().has(mandatId);
  }

  numeleMandatului(m: MandatFunctie): string {
    return m.numeCompletPersoana ?? m.numeCompletConsilier ?? '—';
  }

  formateazaData(iso: string): string {
    const [an, luna, zi] = iso.split('-');
    return `${zi}.${luna}.${an}`;
  }

  formateazaPerioada(dataInceput: string, dataSfarsit: string | null): string {
    return `${this.formateazaData(dataInceput)} – ${dataSfarsit ? this.formateazaData(dataSfarsit) : 'prezent'}`;
  }

  esteButonAdaugaDezactivat(tip: TipFunctie): boolean {
    if (tip === TipFunctie.Primar) return this.existaMandatPrimarActiv();
    if (tip === TipFunctie.SecretarUat) return this.existaSecretarUatActiv();
    return false;
  }

  tooltipButonAdauga(tip: TipFunctie): string {
    if (tip === TipFunctie.Primar && this.existaMandatPrimarActiv()) {
      return 'Există deja un mandat activ de Primar. Închide-l pentru a adăuga altul.';
    }
    if (tip === TipFunctie.SecretarUat && this.existaSecretarUatActiv()) {
      return 'Există deja un mandat activ de Secretar UAT. Închide-l pentru a adăuga altul.';
    }
    return '';
  }

  async adauga(tip: TipFunctie): Promise<void> {
    const data: DateMandatDialog = {
      tipPreselectat: tip,
      existaMandatPrimarActiv: this.existaMandatPrimarActiv(),
      existaSecretarUatActiv: this.existaSecretarUatActiv(),
      consilieriDejaViceprimari: this.consilieriDejaViceprimari()
    };
    const rezultat = await firstValueFrom(
      this.dialog.open(MandatDialog, { data, width: '560px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Mandat adăugat.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  private dataAzi(): string {
    return new Date().toISOString().substring(0, 10);
  }
}