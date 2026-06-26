import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/auth/auth.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { RezultatPunct, TipHcl } from '../../../shared/enums';
import {
  etichetaRezultatPunct, etichetaTipMajoritate, etichetaTipPunct, etichetaTipVot
} from '../../../shared/etichete';
import { ConfirmareDialog, DateConfirmare } from '../../../shared/confirmare/confirmare-dialog';
import { HclService } from '../../hcl/hcl.service';
import { DatePunctDialog, PuncteDialog } from '../puncte-dialog/puncte-dialog';
import { PunctOrdineZi, RezultatVot } from '../puncte.models';
import { actiuniPermise, poateGeneraHcl } from '../puncte.permisiuni';
import { PuncteService } from '../puncte.service';

const COLOANE_BAZA = ['ordine', 'titlu', 'status'];

@Component({
  selector: 'app-puncte-tab',
  imports: [
    MatTableModule, MatIconModule, MatButtonModule, MatMenuModule,
    MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './puncte-tab.html',
  styleUrl: './puncte-tab.scss'
})
export class PuncteTab implements OnInit {
  private readonly api = inject(PuncteService);
  private readonly hclApi = inject(HclService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly sedintaId = input.required<number>();

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly puncte = signal<PunctOrdineZi[]>([]);
  readonly genereazaInCurs = signal(false);

  readonly poateGeneraHcl = poateGeneraHcl;
  readonly TipHcl = TipHcl;

  readonly poateModifica = computed(() => this.auth.areOricareRol('Admin', 'Secretar'));
  readonly coloane = computed(() =>
    this.poateModifica() ? [...COLOANE_BAZA, 'actiuni'] : COLOANE_BAZA);

  readonly etichetaTipPunct = etichetaTipPunct;
  readonly etichetaTipVot = etichetaTipVot;
  readonly etichetaTipMajoritate = etichetaTipMajoritate;
  readonly etichetaRezultatPunct = etichetaRezultatPunct;

  ngOnInit(): void {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      const lista = await this.api.lista(this.sedintaId());
      this.puncte.set([...lista].sort((a, b) => a.ordine - b.ordine));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  actiuni(punct: PunctOrdineZi) {
    return actiuniPermise(punct);
  }

  claseBadge(punct: PunctOrdineZi): string {
    if (!punct.necesitaVot) return '';
    if (punct.rezultat == null) return 'badge badge-deschis';
    switch (punct.rezultat) {
      case RezultatPunct.Adoptat: return 'badge badge-adoptat';
      case RezultatPunct.Respins: return 'badge badge-respins';
      case RezultatPunct.Amanat: return 'badge badge-amanat';
      case RezultatPunct.Retras: return 'badge badge-retras';
    }
  }

  // Precondițiile backend (președinte de ședință + secretar UAT) NU sunt verificabile pe client
  // → reactiv: la eșec, snackbar cu mesajul backend (400). La succes → navigare la hub.
  async genereazaHcl(punct: PunctOrdineZi, tipHcl: TipHcl): Promise<void> {
    if (this.genereazaInCurs()) return;

    this.genereazaInCurs.set(true);
    try {
      const hcl = await this.hclApi.genereaza({ punctOrdineZiId: punct.id, tipHcl });
      this.router.navigate(['/hcl', hcl.id]);
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 6000 });
    } finally {
      this.genereazaInCurs.set(false);
    }
  }

  veziHcl(punct: PunctOrdineZi): void {
    if (punct.hclId != null) this.router.navigate(['/hcl', punct.hclId]);
  }

  async adauga(): Promise<void> {
    const ordineDefault = this.puncte().length === 0
      ? 1
      : Math.max(...this.puncte().map(p => p.ordine)) + 1;
    const data: DatePunctDialog = { sedintaId: this.sedintaId(), ordineDefault };
    const rezultat = await firstValueFrom(
      this.dialog.open(PuncteDialog, { data, width: '600px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Punct adăugat.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  async editeaza(punct: PunctOrdineZi): Promise<void> {
    const data: DatePunctDialog = { sedintaId: this.sedintaId(), punct };
    const rezultat = await firstValueFrom(
      this.dialog.open(PuncteDialog, { data, width: '600px', maxWidth: '95vw' })
        .afterClosed());
    if (!rezultat) return;

    this.snackBar.open('Punct actualizat.', 'Închide', { duration: 4000 });
    await this.incarca();
  }

  async sterge(punct: PunctOrdineZi): Promise<void> {
    const data: DateConfirmare = {
      titlu: 'Ștergere punct',
      mesaj: `Ștergi punctul „${punct.titlu}"?`,
      etichetaConfirmare: 'Șterge',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data, width: '420px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    try {
      await this.api.sterge(this.sedintaId(), punct.id);
      this.snackBar.open('Punct șters.', 'Închide', { duration: 4000 });
      await this.incarca();
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }

  async inchideVot(punct: PunctOrdineZi): Promise<void> {
    const data: DateConfirmare = {
      titlu: 'Închidere vot',
      mesaj: `Închizi votul pentru punctul „${punct.titlu}"? Acțiunea calculează automat rezultatul și e ireversibilă.`,
      etichetaConfirmare: 'Închide votul',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data, width: '480px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    try {
      const rezultat = await this.api.inchideVot(this.sedintaId(), punct.id);
      this.snackBar.open(this.mesajRezultat(punct, rezultat), 'Închide', { duration: 8000 });
      await this.incarca();
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }

  async amana(punct: PunctOrdineZi): Promise<void> {
    const data: DateConfirmare = {
      titlu: 'Amânare punct',
      mesaj: `Amâni punctul „${punct.titlu}"? Acțiunea este finală.`,
      etichetaConfirmare: 'Amână',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data, width: '460px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    try {
      await this.api.amana(this.sedintaId(), punct.id);
      this.snackBar.open('Punct amânat.', 'Închide', { duration: 4000 });
      await this.incarca();
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }

  async retrage(punct: PunctOrdineZi): Promise<void> {
    const data: DateConfirmare = {
      titlu: 'Retragere punct',
      mesaj: `Retragi punctul „${punct.titlu}" de pe ordinea de zi? Acțiunea este finală.`,
      etichetaConfirmare: 'Retrage',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data, width: '460px', maxWidth: '95vw' })
        .afterClosed());
    if (!confirmat) return;

    try {
      await this.api.retrage(this.sedintaId(), punct.id);
      this.snackBar.open('Punct retras.', 'Închide', { duration: 4000 });
      await this.incarca();
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    }
  }

  private mesajRezultat(punct: PunctOrdineZi, r: RezultatVot): string {
    const verdict = etichetaRezultatPunct(r.rezultat);
    return `Punctul „${punct.titlu}" — ${verdict}. ` +
      `${r.voturiPentru} Pentru, ${r.voturiImpotriva} Împotrivă, ${r.voturiAbtinere} Abținere ` +
      `(prag necesar: ${r.pragNecesar} din ${r.totalConsilieriInFunctie}).`;
  }
}