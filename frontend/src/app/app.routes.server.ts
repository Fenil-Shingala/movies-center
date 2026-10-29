import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  {
    path: 'watch/:type/:id',
    renderMode: RenderMode.Client
  },
  {
    path: 'watchlist',
    renderMode: RenderMode.Client
  },
  {
    path: 'history',
    renderMode: RenderMode.Client
  },
  {
    path: 'admin',
    renderMode: RenderMode.Client
  },
  {
    path: 'search',
    renderMode: RenderMode.Client
  },
  {
    path: '**',
    renderMode: RenderMode.Server
  }
];
