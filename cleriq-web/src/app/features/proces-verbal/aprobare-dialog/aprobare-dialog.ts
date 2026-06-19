import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { extrageMesajEroare } from '../../../core/http/erori';
import { formateazaDataOra } from '../../../shared/data';
import { StatusSedinta } from '../../../shared/enums';
import { etichetaStatusSedinta } from '../../../shared/etichete';
import { Sedinta } from '../../sedinte/sedinte.models';
import { SedinteService } from '../../sedinte/sedinte.service';

export interface DateAprobareDialog {
  sedintaPvId: number;
  dataSedintaPv: string;
}

@Component({
  selector: 'app-aprobare-dialog',
  imports: [
    MatDialogModule, MatButtonModule, MatFormFieldModule, MatSelectModule,
    MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './aprobare-dialog.html',
  styleUrl: './aprobare-dialog.scss'
})
export class AprobareDialog implements OnInit {
  private readonly api = inject(SedinteService);
  private readonly dialogRef = inject(MatDialogRef<AprobareDialog, number | null>);
  readonly date = inject<DateAprobareDialog>(MAT_DIALOG_DATA);

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly sedinteDisponibile = signal<Sedinta[]>([]);
  readonly sedintaSelectataId = signal<number | null>(null);

  readonly sedintaSelectata = computed(() => {
    const id = this.sedintaSelectataId();
    return id ? this.sedinteDisponibile().find(s => s.id === id) ?? null : null;
  });

  readonly avertismentCronologic = computed(() => {
    const s = this.sedintaSelectata();
    if (!s) return false;
    return new Date(s.dataOra).getTime() < new Date(this.date.dataSedintaPv).getTime();
  });

  readonly avertismentConvocata = computed(() =>
    this.sedintaSelectata()?.status === StatusSedinta.Convocata);

  readonly formateazaDataOra = formateazaDataOra;
  readonly etichetaStatusSedinta = etichetaStatusSedinta;

  async ngOnInit(): Promise<void> {
    this.seIncarca.set(true);
    try {
      const toate = await this.api.lista();
      this.sedinteDisponibile.set(
        toate
          .filter(s => s.id !== this.date.sedintaPvId)
          .filter(s =>
            s.status === StatusSedinta.Convocata
            || s.status === StatusSedinta.InDesfasurare
            || s.status === StatusSedinta.Finalizata)
          .sort((a, b) => b.dataOra.localeCompare(a.dataOra))
      );
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  confirma(): void {
    const id = this.sedintaSelectataId();
    if (id) this.dialogRef.close(id);
  }

  renunta(): void {
    this.dialogRef.close(null);
  }
}