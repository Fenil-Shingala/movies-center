import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-series',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container page-container">
      <header class="page-header">
        <h1 class="page-title">TV Series</h1>
        <p class="page-subtitle">Discover top rated TV shows, ongoing seasons, and miniseries.</p>
      </header>
      <div class="placeholder-grid">
        @for (item of [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]; track item) {
          <div class="poster-card skeleton"></div>
        }
      </div>
    </div>
  `,
  styles: [`
    .page-container {
      padding-top: calc(var(--nav-height) + 2rem);
      padding-bottom: 4rem;
    }
    .page-header {
      margin-bottom: 2rem;
    }
    .page-title {
      font-size: 2.5rem;
      margin-bottom: 0.5rem;
    }
    .page-subtitle {
      color: var(--text-secondary);
      font-size: 1rem;
      max-width: 600px;
    }
    .placeholder-grid {
      display: grid;
      gap: 1rem;
      grid-template-columns: repeat(3, 1fr);
      @media (min-width: 768px) { grid-template-columns: repeat(4, 1fr); }
      @media (min-width: 1024px) { grid-template-columns: repeat(6, 1fr); }
    }
    .poster-card {
      aspect-ratio: 2 / 3;
      border-radius: var(--radius-sm);
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SeriesComponent {}
