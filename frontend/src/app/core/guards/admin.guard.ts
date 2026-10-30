import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { filter, map, take } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);


  // If already confirmed as Admin (e.g. from cached user profile)
  if (authService.isAdmin()) {
    return true;
  }

  // If auth has already completed its initialization check and user is not admin
  if (typeof authService.isInitialized === 'function' && authService.isInitialized()) {
    authService.openAuthModal('login');
    return router.createUrlTree(['/']);
  }

  // If auth is still initializing (e.g. initial page refresh verifying session), wait for completion
  const initialized$ = authService.isInitialized$;
  if (!initialized$) {
    authService.openAuthModal('login');
    return router.createUrlTree(['/']);
  }

  return initialized$.pipe(
    filter(Boolean),
    take(1),
    map(() => {
      if (authService.isAdmin()) {
        return true;
      }
      authService.openAuthModal('login');
      return router.createUrlTree(['/']);
    })
  );
};
