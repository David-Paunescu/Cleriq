import { Component, computed, inject } from '@angular/core';
import { BreakpointObserver } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from '../../core/auth/auth.service';

interface ElementMeniu {
  eticheta: string;
  icon: string;
  ruta: string;
  roluri?: string[];
}

interface GrupMeniu {
  elemente: ElementMeniu[];
}

const GRUPURI: GrupMeniu[] = [
  {
    elemente: [
      { eticheta: 'Acasă', icon: 'home', ruta: '/' },
      { eticheta: 'Ședințe', icon: 'event', ruta: '/sedinte' }
    ]
  },
  {
    elemente: [
      { eticheta: 'Consilieri', icon: 'groups', ruta: '/consilieri' },
      { eticheta: 'Persoane', icon: 'badge', ruta: '/persoane' },
      { eticheta: 'Funcții oficiale', icon: 'workspace_premium', ruta: '/functii-oficiale' },
      { eticheta: 'Comisii', icon: 'groups_3', ruta: '/comisii' }
    ]
  }
];

@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    MatToolbarModule, MatSidenavModule, MatListModule,
    MatIconModule, MatButtonModule, MatMenuModule, MatDividerModule
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss'
})
export class Shell {
  readonly auth = inject(AuthService);
  private readonly breakpoints = inject(BreakpointObserver);

  readonly esteIngust = toSignal(
    this.breakpoints.observe('(max-width: 959.98px)').pipe(map(r => r.matches)),
    { initialValue: false });

  readonly grupuriMeniu = computed(() =>
    GRUPURI
      .map(g => ({
        elemente: g.elemente.filter(e => !e.roluri || this.auth.areOricareRol(...e.roluri))
      }))
      .filter(g => g.elemente.length > 0));
}