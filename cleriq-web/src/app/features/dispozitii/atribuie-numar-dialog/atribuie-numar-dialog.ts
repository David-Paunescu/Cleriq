import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { extrageMesajEroare } from '../../../core/http/erori';
import { DispozitieDetalii } from '../dispozitii.models';
import { DispozitiiService } from '../dispozitii.service';

// Copie paralelă a atribuie-numar-dialog din HCL (§5) — logică identică (sugestie + lacune + număr luat),
// diferă strict serviciul apelat + tipul returnat.
export interface DateAtribuireNumarDispozitieDialog {
  dispozitieId: number;
}

@Component({
  selector: 'app-atribuie-numar-dispozitie-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatProgressSpinnerModule, MatIconModule
  ],
  templateUrl: './atribuie-numar-dialog.html',
  styleUrl: './atribuie-numar-dialog.scss'
})
export class AtribuieNumarDispozitieDialog {
  private readonly api = inject(DispozitiiService);
  private readonly dialogRef = inject(MatDialogRef<AtribuieNumarDispozitieDialog, DispozitieDetalii>);
  private readonly date = inject<DateAtribuireNumarDispozitieDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seIncarcaSugestie = signal(true);
  readonly seSalveaza = signal(false);
  readonly an = signal<number | null>(null);
  readonly eroare = signal<string | null>(null);
  readonly lacune = signal<number[] | null>(null);
  readonly sugestieAlternativa = signal<number | null>(null);

  readonly form = this.fb.nonNullable.group({
    numar: [null as number | null, [Validators.required, Validators.min(1)]]
  });

  constructor() {
    // Avertismentele devin stale când utilizatorul schimbă numărul → le resetăm.
    this.form.controls.numar.valueChanges.subscribe(() => {
      this.lacune.set(null);
      this.sugestieAlternativa.set(null);
      this.eroare.set(null);
    });
    this.incarcaSugestie();
  }

  private async incarcaSugestie(): Promise<void> {
    this.seIncarcaSugestie.set(true);
    try {
      const s = await this.api.sugestieNumar(this.date.dispozitieId);
      this.an.set(s.an);
      this.form.controls.numar.setValue(s.numar);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarcaSugestie.set(false);
    }
  }

  folosesteSugestia(): void {
    const s = this.sugestieAlternativa();
    if (s != null) this.form.controls.numar.setValue(s);
  }

  async atribuie(confirmaCuLacune = false): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);
    this.sugestieAlternativa.set(null);
    if (!confirmaCuLacune) this.lacune.set(null);

    const numar = this.form.controls.numar.value!;
    try {
      const rezultat = await this.api.atribuieNumar(this.date.dispozitieId, { numar, confirmaCuLacune });
      this.dialogRef.close(rezultat);
    } catch (err) {
      this.trateazaEroare(err);
    } finally {
      this.seSalveaza.set(false);
    }
  }

  private trateazaEroare(err: unknown): void {
    if (err instanceof HttpErrorResponse && err.status === 409 && err.error) {
      const corp = err.error as { mesaj?: string; lacune?: number[]; sugestieAlternativa?: number };
      if (corp.lacune) {
        this.lacune.set(corp.lacune);  // mesaj propriu, mai prietenos, în template
        return;
      }
      if (corp.sugestieAlternativa != null) {
        this.sugestieAlternativa.set(corp.sugestieAlternativa);
        this.eroare.set(corp.mesaj ?? null);
        return;
      }
    }
    this.eroare.set(extrageMesajEroare(err));
  }
}
