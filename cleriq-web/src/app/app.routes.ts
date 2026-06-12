import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then(m => m.Login)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell').then(m => m.Shell),
    children: [
      {
        path: '',
        loadComponent: () => import('./features/acasa/acasa').then(m => m.Acasa)
      },
      {
        path: 'consilieri',
        loadComponent: () =>
          import('./features/consilieri/consilieri-lista/consilieri-lista')
            .then(m => m.ConsilieriLista)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];