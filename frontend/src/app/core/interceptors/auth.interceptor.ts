import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

const TOKEN_KEY = 'mc_access_token';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const injector = inject(Injector);
  let authService: AuthService | null = null;
  try {
    authService = injector.get(AuthService, null, { optional: true });
  } catch {
    // If AuthService is in the process of instantiation, fallback gracefully
    authService = null;
  }

  const token = authService?.token?.() ?? (typeof localStorage !== 'undefined' ? localStorage.getItem(TOKEN_KEY) : null);

  let authReq = req;

  // Include credentials for cookie transmission and attach Bearer token if present
  if (token && !req.headers.has('Authorization')) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(authReq).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !req.url.includes('/auth/login') &&
        !req.url.includes('/auth/register') &&
        !req.url.includes('/auth/refresh')
      ) {
        const resolvedAuth = authService ?? injector.get(AuthService);
        // Attempt silent token rotation and retry once
        return resolvedAuth.refreshToken().pipe(
          switchMap(res => {
            const retryReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${res.accessToken}`
              }
            });
            return next(retryReq);
          }),
          catchError(refreshErr => {
            return throwError(() => refreshErr);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
