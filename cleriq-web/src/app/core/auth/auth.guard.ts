import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = async (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.esteAutentificat()) return true;

  // Access token expirat/absent, dar refresh token posibil valid (revenire după pauză).
  if (await auth.reimprospateaza()) return true;

  return router.createUrlTree(['/login'], { queryParams: { redirect: state.url } });
};