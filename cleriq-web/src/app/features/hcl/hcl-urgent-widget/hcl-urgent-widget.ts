import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { extrageMesajEroare } from '../../../core/http/erori';
import { formateazaDataDoar } from '../../../shared/data';
import { HclUrgent } from '../hcl.models';
import { HclDashboardService } from '../hcl-dashboard.service';

const PRAG = 3;

@Component({
  selector: 'app-hcl-urgent-widget',
  imports: [MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './hcl-urgent-widget.html',
  styleUrl: './hcl-urgent-widget.scss'
})
export class HclUrgentWidget implements OnInit {
  private readonly api = inject(HclDashboardService);
  private readonly router = inject(Router);

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly urgente = signal<HclUrgent[]>([]);

  readonly formateazaDataDoar = formateazaDataDoar;

  ngOnInit(): void {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      this.urgente.set(await this.api.urgentDeComunicat(PRAG));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  deschide(h: HclUrgent): void {
    this.router.navigate(['/hcl', h.hclId]);
  }

  // Cod culoare pe ZileRamase: roșu < 0 (depășit), portocaliu 0–1, verde ≥ 2.
  clasaRand(zileRamase: number): string {
    if (zileRamase < 0) return 'urgent-rand urgent-depasit';
    if (zileRamase <= 1) return 'urgent-rand urgent-aproape';
    return 'urgent-rand urgent-ok';
  }

  textZile(zileRamase: number): string {
    if (zileRamase < 0) return `Depășit cu ${Math.abs(zileRamase)} zile lucrătoare`;
    if (zileRamase === 0) return 'Expiră azi';
    if (zileRamase === 1) return 'O zi lucrătoare rămasă';
    return `${zileRamase} zile lucrătoare rămase`;
  }
}
