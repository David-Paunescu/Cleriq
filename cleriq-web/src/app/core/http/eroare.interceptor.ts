import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const eroareInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const snackBar = inject(MatSnackBar);

  const sesiuneExpirata = () => {
    auth.logout();
    snackBar.open('Sesiunea a expirat. Autentifică-te din nou.', 'Închide', { duration: 5000 });
  };

  return next(req).pipe(
    catchError((eroare: HttpErrorResponse) => {
      // Rutele de auth nu declanșează refresh: 401 la login = parolă greșită,
      // 401 la refresh e tratat în AuthService.
      const esteRutaAuth = req.url.includes('/api/Auth/');

      if (eroare.status === 0) {
        snackBar.open('Serverul nu poate fi contactat. Verifică conexiunea.', 'Închide', { duration: 5000 });
      } else if (eroare.status === 401 && !esteRutaAuth) {
        return from(auth.reimprospateaza()).pipe(
          switchMap(succes => {
            if (!succes) {
              sesiuneExpirata();
              return throwError(() => eroare);
            }

            const reluat = req.clone({
              setHeaders: { Authorization: `Bearer ${auth.token}` }
            });
            return next(reluat).pipe(
              catchError((eroareRetry: HttpErrorResponse) => {
                if (eroareRetry.status === 401) sesiuneExpirata();
                return throwError(() => eroareRetry);
              })
            );
          })
        );
      } else if (eroare.status === 403) {
        snackBar.open('Nu ai permisiunea necesară pentru această acțiune.', 'Închide', { duration: 5000 });
      }

      return throwError(() => eroare);
    })
  );
};