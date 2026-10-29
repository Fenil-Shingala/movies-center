import { HttpInterceptorFn } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';

/**
 * Directs server-side HTTP requests directly to the internal ASP.NET Core backend.
 * This avoids Vite dev-server loopback deadlocks during local development and eliminates
 * proxy latency during SSR rendering in all environments.
 */
export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  const platformId = inject(PLATFORM_ID);

  if (isPlatformServer(platformId) && req.url.startsWith('/api')) {
    const backendOrigin = (typeof process !== 'undefined' && process.env?.['BACKEND_INTERNAL_URL']) || 'http://127.0.0.1:5142';
    const serverReq = req.clone({
      url: `${backendOrigin}${req.url}`
    });
    return next(serverReq);
  }

  return next(req);
};
