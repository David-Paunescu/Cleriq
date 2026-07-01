import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { extrageMesajEroare } from '../../../core/http/erori';
import { formateazaDataScurta } from '../../../shared/data';
import { StatusActRedactional, TipHcl } from '../../../shared/enums';
import { etichetaStatusActRedactional, etichetaTipHcl } from '../../../shared/etichete';
import { normalizeazaPentruCautare } from '../../../shared/text';
import { Hcl } from '../hcl.models';
import { FiltreHcl, HclService } from '../hcl.service';

const COLOANE = ['titlu', 'tip', 'dataAdoptare', 'status'];

@Component({
  selector: 'app-hcl-lista',
  imports: [
    MatTableModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './hcl-lista.html',
  styleUrl: './hcl-lista.scss'
})
export class HclLista {
  private readonly api = inject(HclService);
  private readonly router = inject(Router);

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly hcluri = signal<Hcl[]>([]);

  readonly filtru = signal('');
  readonly anFiltru = signal<number | 'toate'>('toate');
  readonly statusFiltru = signal<StatusActRedactional | 'toate'>('toate');
  readonly tipFiltru = signal<TipHcl | 'toate'>('toate');

  readonly coloane = COLOANE;
  readonly statusuri: StatusActRedactional[] = [
    StatusActRedactional.Draft,
    StatusActRedactional.Numerotat,
    StatusActRedactional.Semnat
  ];
  readonly tipuri: TipHcl[] = [TipHcl.Normativ, TipHcl.Individual];
  // Anii recenți pentru filtrul de numerotare (an curent → an curent − 5).
  readonly ani: number[] = Array.from(
    { length: 6 }, (_, i) => new Date().getFullYear() - i);

  readonly hclFiltrate = computed(() => {
    const termen = normalizeazaPentruCautare(this.filtru());
    return this.hcluri()
      .filter(h => !termen
        || normalizeazaPentruCautare(h.titlu).includes(termen)
        || normalizeazaPentruCautare(this.numarFormatat(h)).includes(termen))
      .sort((a, b) => b.dataAdoptare.localeCompare(a.dataAdoptare));
  });

  constructor() {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);

    const filtre: FiltreHcl = {};
    if (this.anFiltru() !== 'toate') filtre.an = this.anFiltru() as number;
    if (this.statusFiltru() !== 'toate') filtre.status = this.statusFiltru() as StatusActRedactional;
    if (this.tipFiltru() !== 'toate') filtre.tipHcl = this.tipFiltru() as TipHcl;

    try {
      this.hcluri.set(await this.api.lista(filtre));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  laCautare(event: Event): void {
    this.filtru.set((event.target as HTMLInputElement).value);
  }

  // Filtrele an/status/tip sunt server-side → re-fetch la schimbare. Căutarea e client-side.
  laAnSchimbat(v: number | 'toate'): void { this.anFiltru.set(v); this.incarca(); }
  laStatusSchimbat(v: StatusActRedactional | 'toate'): void { this.statusFiltru.set(v); this.incarca(); }
  laTipSchimbat(v: TipHcl | 'toate'): void { this.tipFiltru.set(v); this.incarca(); }

  deschide(h: Hcl): void {
    this.router.navigate(['/hcl', h.id]);
  }

  numarFormatat(h: Hcl): string {
    return h.numar != null && h.anNumerotare != null ? `${h.numar}/${h.anNumerotare}` : '';
  }

  formateaza = formateazaDataScurta;
  etichetaStatus = etichetaStatusActRedactional;
  etichetaTip = etichetaTipHcl;

  claseStatus(status: StatusActRedactional): string {
    switch (status) {
      case StatusActRedactional.Draft: return 'badge badge-draft';
      case StatusActRedactional.Numerotat: return 'badge badge-numerotat';
      case StatusActRedactional.Semnat: return 'badge badge-semnat';
    }
  }
}
