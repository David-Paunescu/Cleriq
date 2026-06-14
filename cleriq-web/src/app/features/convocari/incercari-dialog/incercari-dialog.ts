import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { extrageMesajEroare } from '../../../core/http/erori';
import { formateazaDataOra } from '../../../shared/data';
import { CanalNotificare } from '../../../shared/enums';
import {
  etichetaCanalNotificare, etichetaStatusIncercare
} from '../../../shared/etichete';
import { IncercareTrimitere } from '../convocari.models';
import { ConvocariService } from '../convocari.service';

export interface DateIncercariDialog {
  sedintaId: number;
  convocareId: number;
  numeCompletConsilier: string;
}

@Component({
  selector: 'app-incercari-dialog',
  imports: [
    MatDialogModule, MatTableModule, MatFormFieldModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './incercari-dialog.html',
  styleUrl: './incercari-dialog.scss'
})
export class IncercariDialog implements OnInit {
  private readonly api = inject(ConvocariService);
  readonly date = inject<DateIncercariDialog>(MAT_DIALOG_DATA);

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly incercari = signal<IncercareTrimitere[]>([]);
  readonly filtruCanal = signal<CanalNotificare | 'toate'>('toate');

  readonly incercariFiltrate = computed(() => {
    const canal = this.filtruCanal();
    if (canal === 'toate') return this.incercari();
    return this.incercari().filter(i => i.canal === canal);
  });

  readonly canale = [CanalNotificare.Email, CanalNotificare.Sms];
  readonly coloane = ['canal', 'status', 'destinatar', 'data', 'detalii'];

  readonly etichetaCanalNotificare = etichetaCanalNotificare;
  readonly etichetaStatusIncercare = etichetaStatusIncercare;
  readonly formateazaDataOra = formateazaDataOra;

  ngOnInit(): void {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      const lista = await this.api.listaIncercari(
        this.date.sedintaId, this.date.convocareId);
      this.incercari.set([...lista].sort((a, b) =>
        a.creatLa.localeCompare(b.creatLa)));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  laFiltruCanal(valoare: CanalNotificare | 'toate'): void {
    this.filtruCanal.set(valoare);
  }
}