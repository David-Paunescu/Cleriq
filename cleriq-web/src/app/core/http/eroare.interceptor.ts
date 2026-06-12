import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const eroareInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const snackBar = inject(MatSnackBar);

  return next(req).pipe(
    catchError((eroare: HttpErrorResponse) => {
      const esteLogin = req.url.includes('/api/Auth/login');

      if (eroare.status === 0) {
        snackBar.open('Serverul nu poate fi contactat. Verifică conexiunea.', 'Închide', { duration: 5000 });
      } else if (eroare.status === 401 && !esteLogin) {
        auth.logout();
        snackBar.open('Sesiunea a expirat. Autentifică-te din nou.', 'Închide', { duration: 5000 });
      } else if (eroare.status === 403) {
        snackBar.open('Nu ai permisiunea necesară pentru această acțiune.', 'Închide', { duration: 5000 });
      }

      return throwError(() => eroare);
    })
  );
};