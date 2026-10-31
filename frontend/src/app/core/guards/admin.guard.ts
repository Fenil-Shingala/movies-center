import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { filter, map, take } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // If already confirmed as Admin (e.g. from cached user profile or active session)
  if (authService.isAdmin()) {
    return true;
  }

  const blockAndPrompt = () => {
    if (authService.redirectUrl?.set) {
      authService.redirectUrl.set('/admin');
    }
    // Only prompt login if the user is not authenticated at all
    if (!authService.isAuthenticated?.()) {
      authService.openAuthModal?.('login');
    }
    return router.createUrlTree(['/']);
  };

  // If auth has already completed its initialization check and user is not admin
  if (typeof authService.isInitialized === 'function' && authService.isInitialized()) {
    return blockAndPrompt();
  }

  // If auth is still initializing (e.g. initial page refresh verifying session), wait for completion
  const initialized$ = authService.isInitialized$;
  if (!initialized$) {
    return blockAndPrompt();
  }

  return initialized$.pipe(
    filter(Boolean),
    take(1),
    map(() => {
      if (authService.isAdmin()) {
        return true;
      }
      return blockAndPrompt();
    })
  );
};
