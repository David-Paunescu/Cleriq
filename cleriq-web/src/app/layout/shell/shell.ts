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
import { AuthService } from '../../core/auth/auth.service';

interface ElementMeniu {
  eticheta: string;
  icon: string;
  ruta: string;
  roluri?: string[];   // undefined = vizibil pentru orice utilizator autentificat
}

const MENIU: ElementMeniu[] = [
  { eticheta: 'Acasă', icon: 'home', ruta: '/' },
  { eticheta: 'Consilieri', icon: 'groups', ruta: '/consilieri' }
];

@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet, RouterLink, RouterLinkActive,
    MatToolbarModule, MatSidenavModule, MatListModule,
    MatIconModule, MatButtonModule, MatMenuModule
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

  readonly elementeMeniu = computed(() =>
    MENIU.filter(e => !e.roluri || this.auth.areOricareRol(...e.roluri)));
}