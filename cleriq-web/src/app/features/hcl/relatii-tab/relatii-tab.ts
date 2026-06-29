import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../../core/auth/auth.service';
import { extrageMesajEroare } from '../../../core/http/erori';
import { ConfirmareDialog, DateConfirmare } from '../../../shared/confirmare/confirmare-dialog';
import { etichetaTipRelatieHcl, etichetaTipRelatieHclPasiv } from '../../../shared/etichete';
import { DateRelatieDialog, RelatieDialog } from '../relatie-dialog/relatie-dialog';
import { RelatieHcl } from '../hcl.models';
import { actiuniRelatii } from '../hcl.permisiuni';
import { HclService } from '../hcl.service';

@Component({
  selector: 'app-relatii-tab',
  imports: [
    MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './relatii-tab.html',
  styleUrl: './relatii-tab.scss'
})
export class RelatiiTab implements OnInit {
  private readonly api = inject(HclService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly hclId = input.required<number>();

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly relatiiSursa = signal<RelatieHcl[]>([]);
  readonly relatiiTinta = signal<RelatieHcl[]>([]);
  readonly seStergeId = signal<number | null>(null);

  readonly actiuni = computed(() =>
    actiuniRelatii(this.auth.areOricareRol('Admin', 'Secretar')));

  readonly etichetaTip = etichetaTipRelatieHcl;
  readonly etichetaTipPasiv = etichetaTipRelatieHclPasiv;

  ngOnInit(): void {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      const rezultat = await this.api.relatii(this.hclId());
      this.relatiiSursa.set(rezultat.relatiiSursa);
      this.relatiiTinta.set(rezultat.relatiiTinta);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  async adauga(): Promise<void> {
    if (!this.actiuni().poateAdauga) return;
    const data: DateRelatieDialog = { hclId: this.hclId() };
    const rezultat = await firstValueFrom(
      this.dialog.open<RelatieDialog, DateRelatieDialog, RelatieHcl | undefined>(
        RelatieDialog, { data, width: '480px', maxWidth: '95vw' }).afterClosed());
    if (!rezultat) return;
    this.snackBar.open('Relația a fost adăugată.', 'Închide', { duration: 3000 });
    await this.incarca();
  }

  async sterge(r: RelatieHcl): Promise<void> {
    if (!this.actiuni().poateSterge || this.seStergeId() !== null) return;
    const data: DateConfirmare = {
      titlu: 'Ștergere relație',
      mesaj: 'Relația va fi eliminată (împreună cu oglindirea ei pe actul țintă). Acțiunea nu poate fi anulată.',
      etichetaConfirmare: 'Șterge',
      periculos: true
    };
    const confirmat = await firstValueFrom(
      this.dialog.open(ConfirmareDialog, { data, width: '480px', maxWidth: '95vw' }).afterClosed());
    if (!confirmat) return;

    this.seStergeId.set(r.id);
    try {
      await this.api.stergeRelatie(this.hclId(), r.id);
      this.snackBar.open('Relația a fost ștearsă.', 'Închide', { duration: 3000 });
      await this.incarca();
    } catch (err) {
      this.snackBar.open(extrageMesajEroare(err), 'Închide', { duration: 5000 });
    } finally {
      this.seStergeId.set(null);
    }
  }

  esteIntern(r: RelatieHcl): boolean {
    return r.hclTintaId !== null;
  }

  descriereTinta(r: RelatieHcl): string {
    if (r.hclTintaId !== null) {
      const nr = r.numarTintaFormatat ?? `(draft #${r.hclTintaId})`;
      return r.titluTinta ? `HCL ${nr} — ${r.titluTinta}` : `HCL ${nr}`;
    }
    return r.referintaActExternText ?? '—';
  }

  descriereSursa(r: RelatieHcl): string {
    const nr = r.numarSursaFormatat ?? `(draft #${r.hclSursaId})`;
    return r.titluSursa ? `HCL ${nr} — ${r.titluSursa}` : `HCL ${nr}`;
  }

  deschideHclTinta(r: RelatieHcl): void {
    if (r.hclTintaId !== null) this.router.navigate(['/hcl', r.hclTintaId]);
  }

  deschideHclSursa(r: RelatieHcl): void {
    this.router.navigate(['/hcl', r.hclSursaId]);
  }
}
