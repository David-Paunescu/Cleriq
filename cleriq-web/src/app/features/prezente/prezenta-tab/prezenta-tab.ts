import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatInputModule } from '@angular/material/input';
import { AuthService } from '../../../core/auth/auth.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { ConfirmareDialog, DateConfirmare } from '../../../shared/confirmare/confirmare-dialog';
import { StatusPrezenta } from '../../../shared/enums';
import { etichetaStatusPrezenta } from '../../../shared/etichete';
import { Prezenta } from '../prezente.models';
import { PrezenteService } from '../prezente.service';
import { normalizeazaPentruCautare } from '../../../shared/text';

@Component({
  selector: 'app-prezenta-tab',
  imports: [
    MatTableModule, MatCardModule, MatSelectModule, MatFormFieldModule,
    MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatTooltipModule,
    MatInputModule
  ],
  templateUrl: './prezenta-tab.html',
  styleUrl: './prezenta-tab.scss'
})
export class PrezentaTab implements OnInit {
  private readonly api = inject(PrezenteService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly sedintaId = input.required<number>();

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly prezente = signal<Prezenta[]>([]);
  readonly randuriBlocate = signal<ReadonlySet<number>>(new Set());
  readonly bulkInCurs = signal<{ tip: 'prezent' | 'reset'; procesate: number; total: number } | null>(null);

  readonly poateModifica = computed(() => this.auth.areOricareRol('Admin', 'Secretar'));

  readonly cvorum = computed(() => {
    const lista = this.prezente();
    const total = lista.length;
    const prezentFizic = lista.filter(p => p.status === StatusPrezenta.Prezent).length;
    const prezentOnline = lista.filter(p => p.status === StatusPrezenta.OnlinePrezent).length;
    const absentMotivat = lista.filter(p => p.status === StatusPrezenta.AbsentMotivat).length;
    const absent = lista.filter(p => p.status === StatusPrezenta.Absent).length;
    const prezenti = prezentFizic + prezentOnline;
    const necesar = total === 0 ? 0 : Math.floor(total / 2) + 1;
    return {
      total, prezenti, necesar,
      intrunit: total > 0 && prezenti >= necesar,
      prezentFizic, prezentOnline, absentMotivat, absent
    };
  });

  readonly statusuri: StatusPrezenta[] = [
    StatusPrezenta.Prezent,
    StatusPrezenta.OnlinePrezent,
    StatusPrezenta.AbsentMotivat,
    StatusPrezenta.Absent
  ];

  readonly coloane = ['numeComplet', 'status'];

  readonly StatusPrezenta = StatusPrezenta;
  readonly etichetaStatus = etichetaStatusPrezenta;

  readonly progresPrezent = computed(() => {
    const b = this.bulkInCurs();
    return b?.tip === 'prezent' ? b : null;
  });

  readonly progresReset = computed(() => {
    const b = this.bulkInCurs();
    return b?.tip === 'reset' ? b : null;
  });

  readonly filtru = signal('');

  readonly prezenteFiltrate = computed(() => {
    const termen = normalizeazaPentruCautare(this.filtru());
    if (!termen) return this.prezente();
    return this.prezente().filter(p =>
      normalizeazaPentruCautare(p.numeCompletConsilier).includes(termen));
  });

  ngOnInit(): void {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      this.prezente.set(await this.api.lista(this.sedintaId()));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  async schimbaStatus(prezenta: Prezenta, statusNou: StatusPrezenta): Promise<void> {
    if (statusNou === prezenta.status) return;
    if (this.randuriBlocate().has(prezenta.consilierId)) return;

    this.blocheazaRand(prezenta.consilierId);
    try {
      const rezultat = await this.api.seteaza(this.sedintaId(),
        { consilierId: prezenta.consilierId, status: statusNou, oraSosire: prezenta.oraSosire });
      this.actualizeazaLocal(rezultat);
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.deblocheazaRand(prezenta.consilierId);
    }
  }

async marcheazaTotiPrezenti(): Promise<void> {
    const candidati = this.prezenteFiltrate().filter(p => p.status !== StatusPrezenta.Prezent);
    if (candidati.length === 0) {
      this.snackBar.open('Toți consilierii vizibili sunt deja marcați prezenți.', 'Închide',
        { duration: 4000 });
      return;
    }

    const filtruActiv = this.filtru().trim().length > 0;
    const mesaj = filtruActiv
      ? `Marchezi ${candidati.length} consilier(i) (dintre cei filtrați) ca prezenți fizic? Statusul lor curent va fi suprascris.`
      : `Marchezi ${candidati.length} consilier(i) ca prezenți fizic? Statusul lor curent va fi suprascris.`;

    const date: DateConfirmare = {
      titlu: 'Marcare bulk',
      mesaj,
      etichetaConfirmare: 'Marchează'
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '460px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    this.bulkInCurs.set({ tip: 'prezent', procesate: 0, total: candidati.length });
    let succese = 0;
    let primaEroare: string | null = null;

    for (const p of candidati) {
      try {
        const rezultat = await this.api.seteaza(this.sedintaId(),
          { consilierId: p.consilierId, status: StatusPrezenta.Prezent, oraSosire: p.oraSosire });
        this.actualizeazaLocal(rezultat);
        succese++;
      } catch (err) {
        if (primaEroare === null) primaEroare = extrageMesajEroare(err);
      } finally {
        this.bulkInCurs.update(b => b ? { ...b, procesate: b.procesate + 1 } : b);
      }
    }

    this.bulkInCurs.set(null);
    if (primaEroare !== null) {
      this.snackBar.open(
        `${succese}/${candidati.length} consilieri marcați prezenți. Primă eroare: ${primaEroare}`,
        'Închide', { duration: 8000 });
    } else {
      this.snackBar.open(`${succese} consilier(i) marcați prezenți.`, 'Închide',
        { duration: 4000 });
    }
  }

  esteRandBlocat(consilierId: number): boolean {
    return this.randuriBlocate().has(consilierId);
  }

  iconaStatus(status: StatusPrezenta): string | null {
    switch (status) {
      case StatusPrezenta.Prezent: return 'person';
      case StatusPrezenta.OnlinePrezent: return 'videocam';
      default: return null;
    }
  }

  laCautare(event: Event): void {
    this.filtru.set((event.target as HTMLInputElement).value);
  }

  private actualizeazaLocal(rezultat: Prezenta): void {
    this.prezente.update(lista => lista.map(p =>
      p.consilierId === rezultat.consilierId ? rezultat : p));
  }

  private blocheazaRand(consilierId: number): void {
    this.randuriBlocate.update(s => new Set(s).add(consilierId));
  }

  private deblocheazaRand(consilierId: number): void {
    this.randuriBlocate.update(s => {
      const nou = new Set(s);
      nou.delete(consilierId);
      return nou;
    });
  }

  async reseteazaPrezenta(): Promise<void> {
    const candidati = this.prezenteFiltrate().filter(p => p.status !== StatusPrezenta.Absent);
    if (candidati.length === 0) {
      this.snackBar.open('Toți consilierii vizibili sunt deja marcați absenți.', 'Închide',
        { duration: 4000 });
      return;
    }

    const filtruActiv = this.filtru().trim().length > 0;
    const mesaj = filtruActiv
      ? `Resetezi prezența pentru ${candidati.length} consilier(i) (dintre cei filtrați)? Toți vor fi marcați ca Absent — statusul curent va fi suprascris.`
      : `Resetezi prezența pentru ${candidati.length} consilier(i)? Toți vor fi marcați ca Absent — statusul curent va fi suprascris.`;

    const date: DateConfirmare = {
      titlu: 'Resetare prezență',
      mesaj,
      etichetaConfirmare: 'Resetează',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data: date, width: '480px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    this.bulkInCurs.set({ tip: 'reset', procesate: 0, total: candidati.length });
    let succese = 0;
    let primaEroare: string | null = null;

    for (const p of candidati) {
      try {
        const rezultat = await this.api.seteaza(this.sedintaId(),
          { consilierId: p.consilierId, status: StatusPrezenta.Absent, oraSosire: p.oraSosire });
        this.actualizeazaLocal(rezultat);
        succese++;
      } catch (err) {
        if (primaEroare === null) primaEroare = extrageMesajEroare(err);
      } finally {
        this.bulkInCurs.update(b => b ? { ...b, procesate: b.procesate + 1 } : b);
      }
    }

    this.bulkInCurs.set(null);
    if (primaEroare !== null) {
      this.snackBar.open(
        `${succese}/${candidati.length} consilieri resetați. Primă eroare: ${primaEroare}`,
        'Închide', { duration: 8000 });
    } else {
      this.snackBar.open(`${succese} consilier(i) marcați absenți.`, 'Închide',
        { duration: 4000 });
    }
  }
}