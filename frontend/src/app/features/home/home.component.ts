import { Component, OnInit, signal, inject, PLATFORM_ID, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { 
  LucideClock, 
  LucidePlay, 
  LucideBookmark, 
  LucideX 
} from '@lucide/angular';
import { HomeService } from '../../core/services/home.service';
import { SeoService } from '../../core/services/seo.service';
import { UserDataService } from '../../core/services/user-data.service';
import { HomeFeed, WatchHistoryItem } from '../../core/models/media.models';
import { DEFAULT_BACKDROP_IMAGE, DEFAULT_POSTER_IMAGE } from '../../core/constants/app.constants';
import { HeroBannerComponent } from './components/hero-banner/hero-banner.component';
import { ContentRowComponent } from './components/content-row/content-row.component';
import { TopTenRowComponent } from './components/top-ten-row/top-ten-row.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule, 
    RouterLink, 
    LucideClock, 
    LucidePlay, 
    LucideBookmark, 
    LucideX, 
    HeroBannerComponent, 
    ContentRowComponent, 
    TopTenRowComponent
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent implements OnInit {
  private readonly homeService = inject(HomeService);
  private readonly seoService = inject(SeoService);
  readonly userDataService = inject(UserDataService);
  private readonly platformId = inject(PLATFORM_ID);
  readonly isBrowser = isPlatformBrowser(this.platformId);
  readonly feed = signal<HomeFeed | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.seoService.reset();
    this.loadHomeFeed();
  }

  loadHomeFeed(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.homeService.getHomeFeed().subscribe({
      next: (data) => {
        this.feed.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load home feed', err);
        this.errorMessage.set('Unable to load catalog content. Please check your connection or try again.');
        this.isLoading.set(false);
      }
    });
  }

  getWatchLink(item: WatchHistoryItem): string {
    if (item.type === 'series') {
      const s = item.seasonNumber || 1;
      const e = item.episodeNumber || 1;
      return `/watch/series/${item.titleId}?season=${s}&ep=${e}`;
    }
    return `/watch/movie/${item.titleId}`;
  }

  getPosterUrl(item: WatchHistoryItem): string {
    const path = item.posterPath || item.backdropPath;
    if (!path) return DEFAULT_POSTER_IMAGE;
    const cleanPath = path.startsWith('/') ? path : `/${path}`;
    return `https://image.tmdb.org/t/p/w342${cleanPath}`;
  }

  onPosterError(event: Event): void {
    const img = event.target as HTMLImageElement;
    if (img && !img.src.includes('default-poster.svg')) {
      img.src = DEFAULT_POSTER_IMAGE;
    }
  }

  getThumbnailUrl(path: string | null): string {
    if (!path) return DEFAULT_BACKDROP_IMAGE;
    const cleanPath = path.startsWith('/') ? path : `/${path}`;
    return `https://image.tmdb.org/t/p/w300${cleanPath}`;
  }

  onThumbnailError(event: Event): void {
    const img = event.target as HTMLImageElement;
    if (img && !img.src.includes('default-backdrop.svg')) {
      img.src = DEFAULT_BACKDROP_IMAGE;
    }
  }

  getDaysAgo(dateString: string | undefined | null): string {
    if (!dateString) return 'Watched recently';

    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();

    if (isNaN(diffMs) || diffMs < 0) {
      return 'Watched today';
    }

    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffHours < 24) {
      return 'Watched today';
    }
    if (diffDays === 1) {
      return 'Watched 1 day ago';
    }
    if (diffDays < 30) {
      return `Watched ${diffDays} days ago`;
    }
    const diffMonths = Math.floor(diffDays / 30);
    if (diffMonths === 1) {
      return 'Watched 1 month ago';
    }
    if (diffMonths < 12) {
      return `Watched ${diffMonths} months ago`;
    }
    const diffYears = Math.floor(diffDays / 365);
    return diffYears === 1 ? 'Watched 1 year ago' : `Watched ${diffYears} years ago`;
  }

  isInWatchlist(titleId: number): boolean {
    return this.userDataService.isInWatchlist(titleId);
  }

  toggleWatchlist(item: WatchHistoryItem, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.userDataService.toggleWatchlist(item.titleId, {
      title: item.title,
      posterPath: item.posterPath,
      backdropPath: item.backdropPath,
      type: item.type
    }).subscribe();
  }

  removeFromContinueWatching(item: WatchHistoryItem, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.userDataService.removeFromHistory(item.id, item.titleId).subscribe();
  }
}
