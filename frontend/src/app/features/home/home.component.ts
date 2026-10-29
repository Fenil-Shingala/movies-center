import { Component, OnInit, signal, inject, PLATFORM_ID, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideClock, LucidePlay, LucideFilm } from '@lucide/angular';
import { HomeService } from '../../core/services/home.service';
import { UserDataService } from '../../core/services/user-data.service';
import { HomeFeed, WatchHistoryItem } from '../../core/models/media.models';
import { HeroBannerComponent } from './components/hero-banner/hero-banner.component';
import { ContentRowComponent } from './components/content-row/content-row.component';
import { TopTenRowComponent } from './components/top-ten-row/top-ten-row.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideClock, LucidePlay, LucideFilm, HeroBannerComponent, ContentRowComponent, TopTenRowComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent implements OnInit {
  private readonly homeService = inject(HomeService);
  readonly userDataService = inject(UserDataService);
  private readonly platformId = inject(PLATFORM_ID);

  readonly feed = signal<HomeFeed | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
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

  getThumbnailUrl(path: string | null): string {
    return path ? `https://image.tmdb.org/t/p/w300${path}` : '';
  }
}
