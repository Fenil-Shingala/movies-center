import { 
  Component, 
  OnInit, 
  inject, 
  signal, 
  PLATFORM_ID, 
  ChangeDetectionStrategy 
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { 
  LucideSearch, 
  LucideFilm, 
  LucideTv, 
  LucideX 
} from '@lucide/angular';
import { CatalogService } from '../../core/services/catalog.service';
import { AnalyticsService } from '../../core/services/analytics.service';
import { MediaCard, PagedResult } from '../../core/models/media.models';
import { MediaCardComponent } from '../home/components/media-card/media-card.component';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [
    CommonModule, 
    LucideSearch, 
    LucideFilm, 
    LucideTv, 
    LucideX, 
    MediaCardComponent, 
    PaginationComponent, 
    EmptyStateComponent
  ],
  templateUrl: './search.component.html',
  styleUrl: './search.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SearchComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);
  private readonly analyticsService = inject(AnalyticsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);

  readonly queryText = signal<string>('');
  readonly selectedType = signal<'all' | 'movie' | 'series'>('all');
  readonly currentPage = signal<number>(1);
  readonly titles = signal<MediaCard[]>([]);
  readonly totalCount = signal<number>(0);
  readonly totalPages = signal<number>(1);
  readonly isLoading = signal<boolean>(false);
  readonly hasSearched = signal<boolean>(false);

  readonly skeletonArray = Array.from({ length: 12 });

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const q = params['q'] || '';
      const type = (params['type'] || 'all') as 'all' | 'movie' | 'series';
      const page = params['page'] ? parseInt(params['page'], 10) : 1;

      this.queryText.set(q);
      this.selectedType.set(type);
      this.currentPage.set(page > 0 ? page : 1);

      if (q.trim()) {
        this.executeSearch(q.trim(), type, this.currentPage());
      } else {
        this.titles.set([]);
        this.totalCount.set(0);
        this.totalPages.set(1);
        this.hasSearched.set(false);
      }
    });
  }

  onInputChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.queryText.set(target.value);
  }

  onSearchSubmit(): void {
    const q = this.queryText().trim();
    this.updateQueryParams({ q, page: 1 });
  }

  setType(type: 'all' | 'movie' | 'series'): void {
    if (this.selectedType() !== type) {
      this.selectedType.set(type);
      this.updateQueryParams({ type, page: 1 });
    }
  }

  onPageChange(page: number): void {
    if (page !== this.currentPage()) {
      this.currentPage.set(page);
      this.updateQueryParams({ page });
      if (isPlatformBrowser(this.platformId)) {
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    }
  }

  clearQuery(inputElement?: HTMLInputElement): void {
    this.queryText.set('');
    if (inputElement) {
      inputElement.value = '';
      inputElement.focus();
    }
  }

  clearSearchAndReset(inputElement?: HTMLInputElement): void {
    this.clearQuery(inputElement);
    this.router.navigate(['/search'], { queryParams: {} });
  }

  private executeSearch(query: string, type: 'all' | 'movie' | 'series', page: number): void {
    this.isLoading.set(true);
    this.hasSearched.set(true);

    this.catalogService.searchTitles(query, type, page, 24).subscribe({
      next: (res: PagedResult<MediaCard>) => {
        this.titles.set(res.items);
        this.totalCount.set(res.totalCount);
        this.totalPages.set(res.totalPages > 0 ? res.totalPages : 1);
        this.isLoading.set(false);
        this.analyticsService.trackSearch(query, res.totalCount);
      },
      error: () => {
        this.titles.set([]);
        this.totalCount.set(0);
        this.totalPages.set(1);
        this.isLoading.set(false);
      }
    });
  }

  private updateQueryParams(newParams: { q?: string; type?: string; page?: number }): void {
    const currentParams = this.route.snapshot.queryParams;
    const queryParams: Record<string, string | number | null> = {
      ...currentParams,
      ...newParams
    };

    if (!queryParams['q']) {
      delete queryParams['q'];
    }
    if (queryParams['type'] === 'all') {
      delete queryParams['type'];
    }
    if (queryParams['page'] === 1) {
      delete queryParams['page'];
    }

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: ''
    });
  }
}
