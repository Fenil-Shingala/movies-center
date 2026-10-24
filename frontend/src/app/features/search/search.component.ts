import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideSearch } from '@lucide/angular';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, LucideSearch],
  template: `
    <div class="container page-container">
      <div class="search-box">
        <svg lucideSearch class="search-icon"></svg>
        <input 
          type="search" 
          class="search-input" 
          placeholder="Search movies, TV shows, actors..."
          aria-label="Search"
          autofocus
        />
      </div>
      <div class="search-empty-state">
        <p class="text-muted">Type at least 2 characters to search across our full catalog.</p>
      </div>
    </div>
  `,
  styles: [`
    .page-container {
      padding-top: calc(var(--nav-height) + 2rem);
      padding-bottom: 4rem;
      max-width: 800px;
    }
    .search-box {
      display: flex;
      align-items: center;
      gap: 1rem;
      background-color: var(--bg-surface);
      border: 1px solid var(--border);
      border-radius: var(--radius-md);
      padding: 0.875rem 1.25rem;
      transition: border-color var(--transition-fast);
      &:focus-within {
        border-color: var(--accent);
      }
    }
    .search-icon {
      width: 22px;
      height: 22px;
      color: var(--text-muted);
    }
    .search-input {
      flex: 1;
      background: none;
      border: none;
      color: var(--text-primary);
      font-size: 1.125rem;
      outline: none;
      &::placeholder {
        color: var(--text-muted);
      }
    }
    .search-empty-state {
      margin-top: 3rem;
      text-align: center;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SearchComponent {}
