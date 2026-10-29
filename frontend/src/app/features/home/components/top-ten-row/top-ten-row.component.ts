import { 
  Component, 
  input, 
  signal, 
  viewChild, 
  ElementRef, 
  inject, 
  OnInit, 
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
  LucideChevronRight, 
  LucideFilm, 
  LucideTv 
} from '@lucide/angular';
import { MediaCard } from '../../../../core/models/media.models';
import { HomeService } from '../../../../core/services/home.service';
import { UserDataService } from '../../../../core/services/user-data.service';
import { TMDB_IMAGE_BASE_URL, TMDB_POSTER_SIZE } from '../../../../core/constants/app.constants';

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
    LucideChevronRight, 
    LucideFilm, 
    LucideTv
  ],
  templateUrl: './top-ten-row.component.html',
  styleUrl: './top-ten-row.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TopTenRowComponent implements OnInit {
  private readonly homeService = inject(HomeService);
  readonly userDataService = inject(UserDataService);

  readonly initialItems = input<MediaCard[] | undefined>([]);
  readonly items = signal<MediaCard[]>([]);
  readonly activeTab = signal<'all' | 'movie' | 'series'>('all');
  readonly isLoading = signal<boolean>(false);
  readonly scrollContainer = viewChild<ElementRef<HTMLDivElement>>('scrollContainer');

  ngOnInit(): void {
    const init = this.initialItems();
    if (init && init.length > 0) {
      this.items.set(init);
    } else {
      this.fetchTop10('all');
    }
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
      const scrollAmount = el.clientWidth * 0.75;
      el.scrollBy({ left: -scrollAmount, behavior: 'smooth' });
    }
  }

  scrollRight(): void {
    const el = this.scrollContainer()?.nativeElement;
    if (el) {
      const scrollAmount = el.clientWidth * 0.75;
      el.scrollBy({ left: scrollAmount, behavior: 'smooth' });
    }
  }

  getPosterUrl(path: string | null): string | null {
    if (!path) return null;
    return `${TMDB_IMAGE_BASE_URL}${TMDB_POSTER_SIZE}${path}`;
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
