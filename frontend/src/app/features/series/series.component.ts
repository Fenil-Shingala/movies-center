import { Component, OnInit, inject, signal, computed, ChangeDetectionStrategy, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { CatalogService } from '../../core/services/catalog.service';
import { MediaCard, GenreItem, BrowseFilter, PagedResult } from '../../core/models/media.models';
import { FilterBarComponent } from '../../shared/components/filter-bar/filter-bar.component';
import { PaginationComponent } from '../../shared/components/pagination/pagination.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { MediaCardComponent } from '../../shared/components/media-card';

@Component({
  selector: 'app-series',
  standalone: true,
  imports: [
    CommonModule,
    FilterBarComponent,
    PaginationComponent,
    EmptyStateComponent,
    MediaCardComponent
  ],
  templateUrl: './series.component.html',
  styleUrl: './series.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SeriesComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);

  readonly titles = signal<MediaCard[]>([]);
  readonly genres = signal<GenreItem[]>([]);
  readonly selectedGenre = signal<number | null>(null);
  readonly selectedYear = signal<number | null>(null);
  readonly selectedSort = signal<string>('popularity.desc');
  readonly page = signal<number>(1);
  readonly pageSize = signal<number>(24);
  readonly totalCount = signal<number>(0);
  readonly totalPages = signal<number>(1);
  readonly hasPreviousPage = signal<boolean>(false);
  readonly hasNextPage = signal<boolean>(false);

  readonly loading = signal<boolean>(true);
  readonly error = signal<string | null>(null);

  readonly skeletonCount = computed(() => Array.from({ length: 12 }, (_, i) => i));

  ngOnInit(): void {
    this.loadGenres();

    this.route.queryParamMap.subscribe(params => {
      const genreParam = params.get('genre');
      const yearParam = params.get('year');
      const sortParam = params.get('sort');
      const pageParam = params.get('page');

      this.selectedGenre.set(genreParam ? parseInt(genreParam, 10) : null);
      this.selectedYear.set(yearParam ? parseInt(yearParam, 10) : null);
      this.selectedSort.set(sortParam || 'popularity.desc');
      this.page.set(pageParam ? parseInt(pageParam, 10) : 1);

      if (isPlatformBrowser(this.platformId)) {
        this.loadTitles();
      }
    });
  }

  loadGenres(): void {
    this.catalogService.getGenres().subscribe({
      next: (data) => this.genres.set(data),
      error: () => this.genres.set([])
    });
  }

  loadTitles(): void {
    this.loading.set(true);
    this.error.set(null);

    const filter: BrowseFilter = {
      genre: this.selectedGenre(),
      year: this.selectedYear(),
      sort: this.selectedSort(),
      page: this.page(),
      pageSize: this.pageSize()
    };

    this.catalogService.browseTitles('series', filter).subscribe({
      next: (res: PagedResult<MediaCard>) => {
        this.titles.set(res.items);
        this.totalCount.set(res.totalCount);
        this.totalPages.set(res.totalPages);
        this.hasPreviousPage.set(res.hasPreviousPage);
        this.hasNextPage.set(res.hasNextPage);
        this.loading.set(false);

        if (isPlatformBrowser(this.platformId)) {
          window.scrollTo({ top: 0, behavior: 'smooth' });
        }
      },
      error: (err) => {
        console.error('Failed to load series:', err);
        this.error.set('Failed to load TV series. Please check your connection and try again.');
        this.loading.set(false);
      }
    });
  }

  onGenreChange(genreId: number | null): void {
    this.updateQueryParams({ genre: genreId, page: 1 });
  }

  onYearChange(year: number | null): void {
    this.updateQueryParams({ year, page: 1 });
  }

  onSortChange(sort: string): void {
    this.updateQueryParams({ sort, page: 1 });
  }

  onPageChange(newPage: number): void {
    this.updateQueryParams({ page: newPage });
  }

  onResetFilters(): void {
    this.updateQueryParams({ genre: null, year: null, sort: 'popularity.desc', page: 1 });
  }

  private updateQueryParams(updated: Record<string, string | number | null>): void {
    const currentParams = { ...this.route.snapshot.queryParams };

    for (const [key, value] of Object.entries(updated)) {
      if (value === null || (key === 'page' && value === 1) || (key === 'sort' && value === 'popularity.desc')) {
        delete currentParams[key];
      } else {
        currentParams[key] = value;
      }
    }

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: currentParams
    });
  }
}
