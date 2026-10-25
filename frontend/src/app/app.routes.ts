import { Routes } from '@angular/router';
import { ShellComponent } from './layout/shell/shell.component';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    children: [
      {
        path: '',
        loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent),
        title: 'Movies Center - Watch Movies & TV Series Online Free'
      },
      {
        path: 'movies',
        loadComponent: () => import('./features/movies/movies.component').then(m => m.MoviesComponent),
        title: 'Movies - Movies Center'
      },
      {
        path: 'series',
        loadComponent: () => import('./features/series/series.component').then(m => m.SeriesComponent),
        title: 'TV Series - Movies Center'
      },
      {
        path: 'search',
        loadComponent: () => import('./features/search/search.component').then(m => m.SearchComponent),
        title: 'Search - Movies Center'
      },
      {
        path: 'movie/:id',
        loadComponent: () => import('./features/title-detail/title-detail.component').then(m => m.TitleDetailComponent)
      },
      {
        path: 'series/:id',
        loadComponent: () => import('./features/title-detail/title-detail.component').then(m => m.TitleDetailComponent)
      },
      {
        path: 'title/:id',
        loadComponent: () => import('./features/title-detail/title-detail.component').then(m => m.TitleDetailComponent)
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
