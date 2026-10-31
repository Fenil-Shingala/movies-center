import { 
  Component, 
  input, 
  signal, 
  viewChild, 
  ElementRef, 
  inject, 
  OnInit, 
  AfterViewInit,
  OnDestroy,
  ChangeDetectionStrategy 
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { 
  LucideFlame, 
  LucidePlay, 
  LucideBookmark, 
  LucideCheck, 
  LucideChevronLeft, 
  LucideChevronRight 
} from '@lucide/angular';
import { MediaCard } from '../../../../core/models/media.models';
import { HomeService } from '../../../../core/services/home.service';
import { UserDataService } from '../../../../core/services/user-data.service';
import { TMDB_IMAGE_BASE_URL, TMDB_POSTER_SIZE, DEFAULT_POSTER_IMAGE } from '../../../../core/constants/app.constants';

@Component({
  selector: 'app-top-ten-row',
  standalone: true,
  imports: [
    CommonModule, 
    RouterLink, 
    LucideFlame, 
    LucidePlay, 
    LucideBookmark, 
    LucideCheck, 
    LucideChevronLeft, 
    LucideChevronRight
  ],
  templateUrl: './top-ten-row.component.html',
  styleUrl: './top-ten-row.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TopTenRowComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly homeService = inject(HomeService);
  readonly userDataService = inject(UserDataService);

  readonly initialItems = input<MediaCard[] | undefined>([]);
  readonly items = signal<MediaCard[]>([]);
  readonly activeTab = signal<'all' | 'movie' | 'series'>('all');
  readonly isLoading = signal<boolean>(false);
  readonly scrollContainer = viewChild<ElementRef<HTMLDivElement>>('scrollContainer');

  readonly canScrollLeft = signal<boolean>(false);
  readonly canScrollRight = signal<boolean>(true);

  private resizeObserver?: ResizeObserver;

  ngOnInit(): void {
    const init = this.initialItems();
    if (init && init.length > 0) {
      this.items.set(init);
    } else {
      this.fetchTop10('all');
    }
  }

  ngAfterViewInit(): void {
    this.updateScrollState();

    const el = this.scrollContainer()?.nativeElement;
    if (el && typeof ResizeObserver !== 'undefined') {
      this.resizeObserver = new ResizeObserver(() => {
        this.updateScrollState();
      });
      this.resizeObserver.observe(el);
    }
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }

  updateScrollState(): void {
    const el = this.scrollContainer()?.nativeElement;
    if (!el) return;

    const left = el.scrollLeft ?? 0;
    const clientWidth = el.clientWidth ?? 0;
    const scrollWidth = el.scrollWidth ?? clientWidth;
    const maxScroll = scrollWidth - clientWidth;

    this.canScrollLeft.set(left > 10);
    this.canScrollRight.set(maxScroll > 10 && left < maxScroll - 10);
  }

  setTab(tab: 'all' | 'movie' | 'series'): void {
    if (this.activeTab() === tab) return;
    this.activeTab.set(tab);
    this.fetchTop10(tab);
  }

  private fetchTop10(type: 'all' | 'movie' | 'series'): void {
    this.isLoading.set(true);
    this.homeService.getTop10(type).subscribe({
      next: (data) => {
        this.items.set(data || []);
        this.isLoading.set(false);
        setTimeout(() => this.updateScrollState(), 100);
      },
      error: (err) => {
        console.error('Failed to load top 10', err);
        this.isLoading.set(false);
      }
    });
  }

  scrollLeft(): void {
    const el = this.scrollContainer()?.nativeElement;
    if (el) {
      const scrollAmount = (el.clientWidth || 800) * 0.75;
      el.scrollBy({ left: -scrollAmount, behavior: 'smooth' });
      setTimeout(() => this.updateScrollState(), 350);
    }
  }

  scrollRight(): void {
    const el = this.scrollContainer()?.nativeElement;
    if (el) {
      const scrollAmount = (el.clientWidth || 800) * 0.75;
      el.scrollBy({ left: scrollAmount, behavior: 'smooth' });
      setTimeout(() => this.updateScrollState(), 350);
    }
  }

  getPosterUrl(path: string | null): string {
    if (!path) return DEFAULT_POSTER_IMAGE;
    const cleanPath = path.startsWith('/') ? path : `/${path}`;
    return `${TMDB_IMAGE_BASE_URL}${TMDB_POSTER_SIZE}${cleanPath}`;
  }

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    if (img && !img.src.includes('default-poster.svg')) {
      img.src = DEFAULT_POSTER_IMAGE;
    }
  }

  getWatchLink(item: MediaCard): string {
    return `/watch/${item.type}/${item.tmdbId}`;
  }

  getDetailLink(item: MediaCard): string {
    return `/${item.type}/${item.tmdbId}`;
  }

  isInWatchlist(tmdbId: number): boolean {
    return this.userDataService.isInWatchlist(tmdbId);
  }

  toggleWatchlist(item: MediaCard, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.userDataService.toggleWatchlist(item.tmdbId, {
      title: item.title,
      posterPath: item.posterPath,
      backdropPath: item.backdropPath,
      type: item.type,
      year: item.year,
      rating: item.rating
    }).subscribe();
  }
}
