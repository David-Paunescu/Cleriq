import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { ghidModificariNesalvate } from './core/modificari/ghid-modificari-nesalvate';

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
      },
      {
        path: 'persoane',
        loadComponent: () =>
          import('./features/persoane/persoane-lista/persoane-lista')
            .then(m => m.PersoaneLista)
      },
      {
        path: 'functii-oficiale',
        loadComponent: () =>
          import('./features/functii-oficiale/functii-oficiale-pagina/functii-oficiale-pagina')
            .then(m => m.FunctiiOficialePagina)
      },
      {
        path: 'comisii',
        loadComponent: () =>
          import('./features/comisii/comisii-lista/comisii-lista')
            .then(m => m.ComisiiLista)
      },
      {
        path: 'comisii/:id',
        loadComponent: () =>
          import('./features/comisii/comisie-detalii/comisie-detalii')
            .then(m => m.ComisieDetalii)
      },
      {
      path: 'sedinte',
      loadComponent: () =>
        import('./features/sedinte/sedinte-lista/sedinte-lista')
          .then(m => m.SedinteLista)
      },
      {
        path: 'sedinte/noua',
        loadComponent: () =>
          import('./features/sedinte/sedinta-form/sedinta-form')
            .then(m => m.SedintaForm)
      },
      {
        path: 'sedinte/:id/editeaza',
        loadComponent: () =>
          import('./features/sedinte/sedinta-form/sedinta-form')
            .then(m => m.SedintaForm)
      },
      {
        path: 'sedinte/:id',
        canDeactivate: [ghidModificariNesalvate],
        loadComponent: () =>
          import('./features/sedinte/sedinta-detalii/sedinta-detalii')
            .then(m => m.SedintaDetalii)
      },
      {
        path: 'hcl',
        loadComponent: () =>
          import('./features/hcl/hcl-lista/hcl-lista').then(m => m.HclLista)
      },
      {
        path: 'hcl/:id',
        canDeactivate: [ghidModificariNesalvate],
        loadComponent: () =>
          import('./features/hcl/hcl-detalii/hcl-detalii').then(m => m.HclDetaliiPagina)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];