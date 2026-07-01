import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { AuthService } from '../../../core/auth/auth.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { formateazaDataDoar } from '../../../shared/data';
import { StatusActRedactional, TipDispozitie } from '../../../shared/enums';
import { etichetaStatusActRedactional, etichetaTipDispozitie } from '../../../shared/etichete';
import { normalizeazaPentruCautare } from '../../../shared/text';
import { CreareDispozitieDialog } from '../creare-dispozitie-dialog/creare-dispozitie-dialog';
import { Dispozitie, DispozitieDetalii } from '../dispozitii.models';
import { DispozitiiService, FiltreDispozitii } from '../dispozitii.service';

const COLOANE = ['titlu', 'tip', 'dataEmitere', 'status'];

@Component({
  selector: 'app-dispozitii-lista',
  imports: [
    MatTableModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatIconModule, MatButtonModule, MatProgressSpinnerModule
  ],
  templateUrl: './dispozitii-lista.html',
  styleUrl: './dispozitii-lista.scss'
})
export class DispozitiiLista {
  private readonly api = inject(DispozitiiService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly dispozitii = signal<Dispozitie[]>([]);

  readonly esteAdminSauSecretar = computed(() => this.auth.areOricareRol('Admin', 'Secretar'));

  readonly filtru = signal('');
  readonly anFiltru = signal<number | 'toate'>('toate');
  readonly statusFiltru = signal<StatusActRedactional | 'toate'>('toate');
  readonly tipFiltru = signal<TipDispozitie | 'toate'>('toate');

  readonly coloane = COLOANE;
  readonly statusuri: StatusActRedactional[] = [
    StatusActRedactional.Draft,
    StatusActRedactional.Numerotat,
    StatusActRedactional.Semnat
  ];
  readonly tipuri: TipDispozitie[] = [TipDispozitie.Normativ, TipDispozitie.Individual];
  // Anii recenți pentru filtrul de numerotare (an curent → an curent − 5).
  readonly ani: number[] = Array.from({ length: 6 }, (_, i) => new Date().getFullYear() - i);

  readonly dispozitiiFiltrate = computed(() => {
    const termen = normalizeazaPentruCautare(this.filtru());
    return this.dispozitii()
      .filter(d => !termen
        || normalizeazaPentruCautare(d.titlu).includes(termen)
        || normalizeazaPentruCautare(this.numarFormatat(d)).includes(termen))
      .sort((a, b) => b.dataEmitere.localeCompare(a.dataEmitere));
  });

  constructor() {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);

    const filtre: FiltreDispozitii = {};
    if (this.anFiltru() !== 'toate') filtre.an = this.anFiltru() as number;
    if (this.statusFiltru() !== 'toate') filtre.status = this.statusFiltru() as StatusActRedactional;
    if (this.tipFiltru() !== 'toate') filtre.tip = this.tipFiltru() as TipDispozitie;

    try {
      this.dispozitii.set(await this.api.lista(filtre));
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
  laTipSchimbat(v: TipDispozitie | 'toate'): void { this.tipFiltru.set(v); this.incarca(); }

  async deschideCreare(): Promise<void> {
    const rezultat = await firstValueFrom(
      this.dialog.open<CreareDispozitieDialog, undefined, DispozitieDetalii | undefined>(
        CreareDispozitieDialog, { width: '480px', maxWidth: '95vw' }).afterClosed());
    if (!rezultat) return;
    // Creeaza întoarce detaliul complet → navigăm direct la hub-ul noii dispoziții.
    this.router.navigate(['/dispozitii', rezultat.id]);
  }

  deschide(d: Dispozitie): void {
    this.router.navigate(['/dispozitii', d.id]);
  }

  numarFormatat(d: Dispozitie): string {
    return d.numar != null && d.anNumerotare != null ? `${d.numar}/${d.anNumerotare}` : '';
  }

  // Dispoziție de convocare = legată de o ședință (badge pe listă, link în detaliu).
  esteConvocare(d: Dispozitie): boolean {
    return d.sedintaId != null;
  }

  formateaza = formateazaDataDoar;
  etichetaStatus = etichetaStatusActRedactional;
  etichetaTip = etichetaTipDispozitie;

  claseStatus(status: StatusActRedactional): string {
    switch (status) {
      case StatusActRedactional.Draft: return 'badge badge-draft';
      case StatusActRedactional.Numerotat: return 'badge badge-numerotat';
      case StatusActRedactional.Semnat: return 'badge badge-semnat';
    }
  }
}
