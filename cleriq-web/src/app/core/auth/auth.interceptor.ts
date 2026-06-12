import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token;

  // Tokenul se atașează DOAR către API-ul propriu, niciodată spre URL-uri externe.
  const esteApiProprie = req.url.startsWith(`${environment.apiUrl}/api`);

  if (token && esteApiProprie) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }

  return next(req);
};