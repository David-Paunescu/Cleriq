import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { extrageMesajEroare } from '../../../core/http/erori';
import { TipRelatieHcl } from '../../../shared/enums';
import { etichetaTipRelatieHcl } from '../../../shared/etichete';
import { normalizeazaPentruCautare } from '../../../shared/text';
import { Hcl, RelatieHcl } from '../hcl.models';
import { HclService } from '../hcl.service';

export interface DateRelatieDialog {
  hclId: number;
}

@Component({
  selector: 'app-relatie-dialog',
  imports: [
    ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatRadioModule, MatAutocompleteModule, MatButtonModule,
    MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './relatie-dialog.html',
  styleUrl: './relatie-dialog.scss'
})
export class RelatieDialog {
  private readonly api = inject(HclService);
  private readonly dialogRef = inject(MatDialogRef<RelatieDialog, RelatieHcl>);
  private readonly date = inject<DateRelatieDialog>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly seIncarcaHcluri = signal(true);
  readonly seSalveaza = signal(false);
  readonly eroare = signal<string | null>(null);
  private readonly hcluri = signal<Hcl[]>([]);

  readonly etichetaTip = etichetaTipRelatieHcl;
  readonly tipuri: TipRelatieHcl[] = [
    TipRelatieHcl.Modifica, TipRelatieHcl.Abroga, TipRelatieHcl.Suspenda,
    TipRelatieHcl.PuneInAplicare, TipRelatieHcl.Completeaza, TipRelatieHcl.Republica
  ];

  readonly form = this.fb.nonNullable.group({
    mod: ['intern' as 'intern' | 'extern', Validators.required],
    tipRelatie: [TipRelatieHcl.Modifica, Validators.required],
    hclTinta: [null as Hcl | string | null],
    referintaActExternText: ['']
  });

  readonly modCurent = toSignal(this.form.controls.mod.valueChanges,
    { initialValue: this.form.controls.mod.value });

  private readonly cautareTinta = toSignal(this.form.controls.hclTinta.valueChanges,
    { initialValue: this.form.controls.hclTinta.value });

  readonly hcluriFiltrate = computed(() => {
    const v = this.cautareTinta();
    const termen = typeof v === 'string' ? normalizeazaPentruCautare(v) : '';
    return this.hcluri()
      .filter(h => h.id !== this.date.hclId)
      .filter(h => !termen
        || normalizeazaPentruCautare(h.titlu).includes(termen)
        || normalizeazaPentruCautare(this.numarFormatat(h)).includes(termen))
      .slice(0, 50);
  });

  constructor() {
    this.incarcaHcluri();
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      if (this.eroare()) this.eroare.set(null);
    });
  }

  afiseazaHcl = (h: Hcl | string | null): string =>
    h && typeof h === 'object' ? `HCL ${this.numarFormatat(h)} — ${h.titlu}` : '';

  numarFormatat(h: Hcl): string {
    return h.numar != null && h.anNumerotare != null
      ? `${h.numar}/${h.anNumerotare}` : `(draft #${h.id})`;
  }

  async adauga(): Promise<void> {
    const v = this.form.getRawValue();
    let hclTintaId: number | null = null;
    let referinta: string | null = null;

    if (v.mod === 'intern') {
      if (!v.hclTinta || typeof v.hclTinta !== 'object') {
        this.eroare.set('Selectează o hotărâre internă din listă.');
        return;
      }
      hclTintaId = v.hclTinta.id;
    } else {
      referinta = v.referintaActExternText.trim();
      if (!referinta) {
        this.eroare.set('Introdu referința actului extern.');
        return;
      }
      if (referinta.length > 300) {
        this.eroare.set('Referința nu poate depăși 300 de caractere.');
        return;
      }
    }

    this.seSalveaza.set(true);
    this.eroare.set(null);
    try {
      const rezultat = await this.api.adaugaRelatie(this.date.hclId, {
        hclTintaId,
        referintaActExternText: referinta,
        tipRelatie: v.tipRelatie
      });
      this.dialogRef.close(rezultat);
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seSalveaza.set(false);
    }
  }

  private async incarcaHcluri(): Promise<void> {
    try {
      // take=200 (plafonul backend) — filtrarea autocomplete e pe client.
      // TODO: peste ~200 HCL/instituție → endpoint de căutare server-side (după număr/an/titlu).
      this.hcluri.set(await this.api.lista({ take: 200 }));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarcaHcluri.set(false);
    }
  }
}
