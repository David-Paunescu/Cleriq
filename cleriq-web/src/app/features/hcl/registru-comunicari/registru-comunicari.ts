import { Component, OnInit, inject, signal } from '@angular/core';
import { Location } from '@angular/common';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { extrageMesajEroare } from '../../../core/http/erori';
import { formateazaDataDoar } from '../../../shared/data';
import { RaspunsPrefect } from '../../../shared/enums';
import { etichetaCanalTransmiterePrefect, etichetaRaspunsPrefect } from '../../../shared/etichete';
import { RegistruComunicare } from '../hcl.models';
import { RegistruComunicariService } from './registru-comunicari.service';

const COLOANE = ['nrOrdine', 'dataTrimiteri', 'hcl', 'canal', 'raspuns'];
const PE_PAGINA = 50;

@Component({
  selector: 'app-registru-comunicari',
  imports: [
    MatTableModule, MatFormFieldModule, MatSelectModule, MatIconModule,
    MatButtonModule, MatProgressSpinnerModule, MatTooltipModule
  ],
  templateUrl: './registru-comunicari.html',
  styleUrl: './registru-comunicari.scss'
})
export class RegistruComunicari implements OnInit {
  private readonly api = inject(RegistruComunicariService);
  private readonly router = inject(Router);
  private readonly location = inject(Location);

  readonly seIncarca = signal(false);
  readonly eroare = signal<string | null>(null);
  readonly randuri = signal<RegistruComunicare[]>([]);
  readonly an = signal<number>(new Date().getFullYear());
  readonly pagina = signal(1);
  readonly areUrmator = signal(false);

  readonly coloane = COLOANE;
  // An curent → an curent − 5 (paritate cu filtrul din hcl-lista).
  readonly ani: number[] = Array.from({ length: 6 }, (_, i) => new Date().getFullYear() - i);

  readonly etichetaCanal = etichetaCanalTransmiterePrefect;
  readonly etichetaRaspuns = etichetaRaspunsPrefect;
  readonly formateazaDataDoar = formateazaDataDoar;

  ngOnInit(): void {
    this.incarca();
  }

  async incarca(): Promise<void> {
    this.seIncarca.set(true);
    this.eroare.set(null);
    try {
      // Cerem PE_PAGINA + 1: dacă vine rândul în plus, există pagină următoare (backend nu dă count).
      const lista = await this.api.lista({
        an: this.an(), page: this.pagina(), size: PE_PAGINA + 1
      });
      this.areUrmator.set(lista.length > PE_PAGINA);
      this.randuri.set(lista.slice(0, PE_PAGINA));
    } catch (err) {
      this.eroare.set(extrageMesajEroare(err));
    } finally {
      this.seIncarca.set(false);
    }
  }

  laAnSchimbat(an: number): void {
    this.an.set(an);
    this.pagina.set(1);
    this.incarca();
  }

  paginaAnterioara(): void {
    if (this.pagina() <= 1) return;
    this.pagina.update(p => p - 1);
    this.incarca();
  }

  paginaUrmatoare(): void {
    if (!this.areUrmator()) return;
    this.pagina.update(p => p + 1);
    this.incarca();
  }

  deschideHcl(r: RegistruComunicare): void {
    this.router.navigate(['/hcl', r.hclId]);
  }

  inapoi(): void {
    this.location.back();
  }

  claseBadgeRaspuns(r: RaspunsPrefect | null): string {
    if (r === null) return 'badge badge-neutru';
    switch (r) {
      case RaspunsPrefect.Acceptat: return 'badge badge-acceptat';
      case RaspunsPrefect.RespinsLegalitate: return 'badge badge-respins';
      case RaspunsPrefect.CereClarificari: return 'badge badge-clarificari';
      case RaspunsPrefect.FaraRaspuns: return 'badge badge-neutru';
    }
  }
}
