import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/auth/auth.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { OptiuneVot, RezultatPunct, StatusPrezenta, StatusSedinta, TipVot } from '../../../shared/enums';
import {
  etichetaOptiuneVot, etichetaRezultatPunct, etichetaStatusSedinta
} from '../../../shared/etichete';
import { Prezenta } from '../../prezente/prezente.models';
import { PrezenteService } from '../../prezente/prezente.service';
import { PunctOrdineZi } from '../../puncte/puncte.models';
import { PuncteService } from '../../puncte/puncte.service';
import { VoturiPunct } from '../voturi.models';
import { VoturiService } from '../voturi.service';

type ModBody =
  | 'rezultat-inchis'
  | 'tabel-management'
  | 'chip-self'
  | 'mesaj-secret'
  | 'mesaj-neprezent';

@Component({
  selector: 'app-voturi-tab',
  imports: [
    MatExpansionModule, MatButtonToggleModule, MatButtonModule,
    MatIconModule, MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './voturi-tab.html',
  styleUrl: './voturi-tab.scss'
})
export class VoturiTab implements OnInit {
  private readonly apiVoturi = inject(VoturiService);
  private readonly apiPuncte = inject(PuncteService);
  private readonly apiPrezente = inject(PrezenteService);
  private readonly auth = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);

  readonly sedintaId = input.required<number>();
  readonly statusSedinta = input.required<StatusSedinta>();

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly puncte = signal<PunctOrdineZi[]>([]);
  readonly voturi = signal<Map<number, VoturiPunct>>(new Map());
  readonly prezente = signal<Prezenta[]>([]);
  readonly panourileExpandate = signal<Set<number>>(new Set());
  readonly chipuriBlocate = signal<Set<string>>(new Set());

  readonly consilierIdLogat = computed(() => this.auth.utilizator()?.consilierId ?? null);
  readonly esteConsilier = computed(() => this.auth.areRol('Consilier'));
  readonly esteAdminSauSecretar = computed(() => this.auth.areOricareRol('Admin', 'Secretar'));

  readonly esteConsilierLogatPrezent = computed(() => {
    const cid = this.consilierIdLogat();
    if (cid === null) return false;
    return this.prezente().some(p =>
      p.consilierId === cid
      && (p.status === StatusPrezenta.Prezent || p.status === StatusPrezenta.OnlinePrezent));
  });

  readonly totExpandat = computed(() =>
    this.puncte().length > 0
    && this.panourileExpandate().size === this.puncte().length);

  readonly afiseazaBannerStatus = computed(() => {
    const s = this.statusSedinta();
    return s !== StatusSedinta.Convocata && s !== StatusSedinta.InDesfasurare;
  });

  readonly OptiuneVot = OptiuneVot;
  readonly StatusPrezenta = StatusPrezenta;
  readonly TipVot = TipVot;
  readonly etichetaOptiuneVot = etichetaOptiuneVot;
  readonly etichetaRezultatPunct = etichetaRezultatPunct;
  readonly etichetaStatusSedinta = etichetaStatusSedinta;
  readonly optiuni: OptiuneVot[] = [OptiuneVot.Pentru, OptiuneVot.Impotriva, OptiuneVot.Abtinere];

  ngOnInit(): void {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      const [punctele, prezenta] = await Promise.all([
        this.apiPuncte.lista(this.sedintaId()),
        this.apiPrezente.lista(this.sedintaId())
      ]);

      const cuVot = punctele
        .filter(p => p.necesitaVot)
        .sort((a, b) => a.ordine - b.ordine);

      const voturiArr = await Promise.all(
        cuVot.map(p => this.apiVoturi.lista(this.sedintaId(), p.id)));
      const voturiMap = new Map<number, VoturiPunct>();
      cuVot.forEach((p, i) => voturiMap.set(p.id, voturiArr[i]));

      this.puncte.set(cuVot);
      this.prezente.set(prezenta);
      this.voturi.set(voturiMap);
      this.panourileExpandate.set(new Set(
        cuVot.filter(p => p.rezultat == null).map(p => p.id)
      ));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  modBody(punct: PunctOrdineZi): ModBody {
    if (punct.rezultat != null) return 'rezultat-inchis';
    if (this.esteConsilier()) {
      if (!this.esteConsilierLogatPrezent()) return 'mesaj-neprezent';
      return 'chip-self';
    }
    if (punct.tipVot === TipVot.Secret) return 'mesaj-secret';
    return 'tabel-management';
  }

  voturiPunct(punctId: number): VoturiPunct | null {
    return this.voturi().get(punctId) ?? null;
  }

  consilieriPrezenti(): Prezenta[] {
    return this.prezente().filter(p =>
      p.status === StatusPrezenta.Prezent || p.status === StatusPrezenta.OnlinePrezent);
  }

  optiuneConsilierPe(punctId: number, consilierId: number): OptiuneVot | null {
    const vp = this.voturi().get(punctId);
    if (!vp) return null;
    return vp.voturiNominale.find(v => v.consilierId === consilierId)?.optiune ?? null;
  }

  optiuneSelfPe(punctId: number): OptiuneVot | null {
    return this.voturi().get(punctId)?.votulMeu ?? null;
  }

  esteChipBlocat(cheie: string): boolean {
    return this.chipuriBlocate().has(cheie);
  }

  esteExpandat(punctId: number): boolean {
    return this.panourileExpandate().has(punctId);
  }

  setExpandat(punctId: number, expandat: boolean): void {
    this.panourileExpandate.update(s => {
      const nou = new Set(s);
      if (expandat) nou.add(punctId);
      else nou.delete(punctId);
      return nou;
    });
  }

  comutaExpandareTot(): void {
    if (this.totExpandat()) {
      this.panourileExpandate.set(new Set());
    } else {
      this.panourileExpandate.set(new Set(this.puncte().map(p => p.id)));
    }
  }

  iconaPrezenta(status: StatusPrezenta): string {
    return status === StatusPrezenta.OnlinePrezent ? 'videocam' : 'person';
  }

  claseBadge(punct: PunctOrdineZi): string {
    if (punct.rezultat == null) return 'badge badge-deschis';
    switch (punct.rezultat) {
      case RezultatPunct.Adoptat: return 'badge badge-adoptat';
      case RezultatPunct.Respins: return 'badge badge-respins';
      case RezultatPunct.Amanat: return 'badge badge-amanat';
      case RezultatPunct.Retras: return 'badge badge-retras';
    }
  }

  claseBadgeOptiune(o: OptiuneVot): string {
    switch (o) {
      case OptiuneVot.Pentru: return 'badge-optiune-pentru';
      case OptiuneVot.Impotriva: return 'badge-optiune-impotriva';
      case OptiuneVot.Abtinere: return 'badge-optiune-abtinere';
    }
  }

  async voteazaManual(punct: PunctOrdineZi, consilierId: number, optiune: OptiuneVot): Promise<void> {
    const cheie = `${punct.id}-${consilierId}`;
    if (this.chipuriBlocate().has(cheie)) return;
    this.blocheaza(cheie);
    try {
      await this.apiVoturi.inregistreaza(this.sedintaId(), punct.id, { consilierId, optiune });
      await this.reincarcaVoturi(punct.id);
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.deblocheaza(cheie);
    }
  }

  async voteazaSelf(punct: PunctOrdineZi, optiune: OptiuneVot): Promise<void> {
    const cheie = `${punct.id}-self`;
    if (this.chipuriBlocate().has(cheie)) return;
    this.blocheaza(cheie);
    try {
      await this.apiVoturi.inregistreazaSelf(this.sedintaId(), punct.id, { optiune });
      await this.reincarcaVoturi(punct.id);
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.deblocheaza(cheie);
    }
  }

  private async reincarcaVoturi(punctId: number): Promise<void> {
    try {
      const noi = await this.apiVoturi.lista(this.sedintaId(), punctId);
      this.voturi.update(m => new Map(m).set(punctId, noi));
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }

  private blocheaza(cheie: string): void {
    this.chipuriBlocate.update(s => new Set(s).add(cheie));
  }

  private deblocheaza(cheie: string): void {
    this.chipuriBlocate.update(s => {
      const nou = new Set(s);
      nou.delete(cheie);
      return nou;
    });
  }
}