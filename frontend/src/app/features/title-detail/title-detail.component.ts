import { 
  Component, 
  OnInit, 
  inject, 
  signal, 
  computed, 
  HostListener, 
  ChangeDetectionStrategy,
  effect 
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DomSanitizer, SafeResourceUrl, Title } from '@angular/platform-browser';
import { 
  LucidePlay, 
  LucideStar, 
  LucideClock, 
  LucideFilm, 
  LucideTv, 
  LucideBookmark, 
  LucideUsers, 
  LucideX, 
  LucideTrash2,
  LucideSparkles
} from '@lucide/angular';
import { CatalogService } from '../../core/services/catalog.service';
import { UserDataService } from '../../core/services/user-data.service';
import { AuthService } from '../../core/services/auth.service';
import { 
  TitleDetail, 
  SeasonItem, 
  VideoItem, 
  EpisodeItem, 
  RatingDto, 
  MediaCard 
} from '../../core/models/media.models';
import { MediaCardComponent } from '../../shared/components/media-card';
import { 
  TMDB_IMAGE_BASE_URL, 
  TMDB_POSTER_SIZE, 
  TMDB_BACKDROP_SIZE,
  TMDB_PROFILE_SIZE,
  TMDB_STILL_SIZE
} from '../../core/constants/app.constants';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { CustomSelectComponent, SelectOption } from '../../shared/components/custom-select/custom-select.component';
import { CommunityReviewsComponent } from './components/community-reviews/community-reviews.component';

@Component({
  selector: 'app-title-detail',
  standalone: true,
  imports: [
    CommonModule, 
    RouterLink, 
    CustomSelectComponent,
    LucidePlay, 
    LucideStar, 
    LucideClock, 
    LucideFilm, 
    LucideTv, 
    LucideBookmark, 
    LucideUsers, 
    LucideX, 
    LucideTrash2,
    LucideSparkles,
    EmptyStateComponent,
    MediaCardComponent,
    CommunityReviewsComponent
  ],
  templateUrl: './title-detail.component.html',
  styleUrl: './title-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TitleDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly catalogService = inject(CatalogService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly titleService = inject(Title);
  readonly userDataService = inject(UserDataService);
  readonly authService = inject(AuthService);

  readonly title = signal<TitleDetail | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly selectedSeasonNumber = signal<number>(1);
  readonly activeTrailer = signal<VideoItem | null>(null);

  // Recommendations State
  readonly recommendations = signal<MediaCard[]>([]);
  readonly isLoadingRecommendations = signal<boolean>(false);

  // Ratings State
  readonly ratingData = signal<RatingDto | null>(null);
  readonly hoverRating = signal<number>(0);
  readonly isSubmittingRating = signal<boolean>(false);

  readonly userRating = computed(() => this.ratingData()?.userRating ?? null);
  readonly averageRating = computed(() => this.ratingData()?.averageRating ?? 0);
  readonly totalRatings = computed(() => this.ratingData()?.totalRatings ?? 0);

  constructor() {
    effect(() => {
      // Re-fetch ratings whenever the logged-in user changes
      const user = this.authService.currentUser();
      const t = this.title();
      if (t) {
        this.loadRating(t.id);
      }
    });
  }

  readonly isInWatchlist = computed(() => {
    const t = this.title();
    return t ? this.userDataService.isInWatchlist(t.id) : false;
  });

  readonly backdropUrl = computed(() => {
    const item = this.title();
    return item?.backdropPath ? `${TMDB_IMAGE_BASE_URL}${TMDB_BACKDROP_SIZE}${item.backdropPath}` : null;
  });

  readonly posterUrl = computed(() => {
    const item = this.title();
    return item?.posterPath ? `${TMDB_IMAGE_BASE_URL}${TMDB_POSTER_SIZE}${item.posterPath}` : null;
  });

  readonly activeSeason = computed(() => {
    const item = this.title();
    if (!item || item.type !== 'series' || !item.seasons) return null;
    return item.seasons.find(s => s.seasonNumber === this.selectedSeasonNumber()) ?? item.seasons[0] ?? null;
  });

  readonly trailerVideos = computed(() => {
    const item = this.title();
    if (!item?.videos) return [];
    return item.videos.filter(v => v.site === 'YouTube' && (v.type === 'Trailer' || v.type === 'Teaser'));
  });

  readonly formattedRuntime = computed(() => {
    const mins = this.title()?.runtimeMinutes;
    if (!mins) return null;
    const h = Math.floor(mins / 60);
    const m = mins % 60;
    return h > 0 ? `${h}h ${m}m` : `${m}m`;
  });

  readonly watchLink = computed(() => {
    const item = this.title();
    if (!item) return ['/watch'];
    return ['/watch', item.type, item.tmdbId];
  });

  readonly primaryTrailer = computed(() => {
    const trailers = this.trailerVideos();
    return trailers.find(t => t.type === 'Trailer') ?? trailers[0] ?? null;
  });

  readonly mainTrailer = this.primaryTrailer;

  readonly seasonOptions = computed<SelectOption[]>(() => {
    const item = this.title();
    if (!item?.seasons) return [];
    return item.seasons
      .filter(s => s.seasonNumber > 0)
      .map(s => ({
        value: s.seasonNumber,
        label: s.name || `Season ${s.seasonNumber}`
      }));
  });

  onSeasonSelect(val: string | number): void {
    const seasonNumber = typeof val === 'number' ? val : parseInt(val, 10);
    this.selectedSeasonNumber.set(seasonNumber);
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      if (idParam) {
        const id = parseInt(idParam, 10);
        if (!isNaN(id) && id > 0) {
          if (typeof window !== 'undefined') {
            window.scrollTo({ top: 0, behavior: 'smooth' });
          }
          this.loadTitle(id);
        } else {
          this.isLoading.set(false);
          this.title.set(null);
        }
      }
    });
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.activeTrailer()) {
      this.closeTrailer();
    }
  }

  loadTitle(id: number): void {
    this.isLoading.set(true);
    this.catalogService.getTitleDetail(id).subscribe({
      next: (detail: TitleDetail) => {
        this.title.set(detail);
        this.isLoading.set(false);

        // Update document title for SEO
        if (detail.year) {
          this.titleService.setTitle(`${detail.title} (${detail.year}) - Movies Center`);
        } else {
          this.titleService.setTitle(`${detail.title} - Movies Center`);
        }

        // Set default season if series has seasons
        if (detail.type === 'series' && detail.seasons.length > 0) {
          const firstSeason = detail.seasons.find(s => s.seasonNumber > 0) ?? detail.seasons[0];
          this.selectedSeasonNumber.set(firstSeason.seasonNumber);
        }

        // Load community ratings & recommendations
        this.loadRating(detail.id);
        this.loadRecommendations(detail.id);
      },
      error: () => {
        this.title.set(null);
        this.recommendations.set([]);
        this.isLoading.set(false);
      }
    });
  }

  loadRecommendations(id: number): void {
    this.isLoadingRecommendations.set(true);
    this.catalogService.getTitleRecommendations(id, 12).subscribe({
      next: (recs) => {
        this.recommendations.set(recs);
        this.isLoadingRecommendations.set(false);
      },
      error: () => {
        this.recommendations.set([]);
        this.isLoadingRecommendations.set(false);
      }
    });
  }

  // Ratings Methods
  loadRating(id: number): void {
    this.catalogService.getTitleRating(id).subscribe({
      next: (data) => this.ratingData.set(data),
      error: () => this.ratingData.set(null)
    });
  }

  getDistributionPercent(score: number): number {
    const data = this.ratingData();
    if (!data || data.totalRatings === 0) return 0;
    const count = data.distribution[score] ?? 0;
    return Math.round((count / data.totalRatings) * 100);
  }

  getDistributionCount(score: number): number {
    return this.ratingData()?.distribution[score] ?? 0;
  }

  getProviderLogoUrl(path: string): string {
    return `https://image.tmdb.org/t/p/w92${path}`;
  }

  onStarHover(score: number): void {
    this.hoverRating.set(score);
  }

  onStarLeave(): void {
    this.hoverRating.set(0);
  }

  rateTitle(score: number): void {
    if (!this.authService.isAuthenticated()) {
      this.authService.openAuthModal('login');
      return;
    }

    const t = this.title();
    if (!t) return;

    this.isSubmittingRating.set(true);
    this.catalogService.submitRating(t.id, score).subscribe({
      next: (updatedRating: RatingDto) => {
        this.ratingData.set(updatedRating);
        this.isSubmittingRating.set(false);
      },
      error: () => {
        this.isSubmittingRating.set(false);
      }
    });
  }

  removeRating(): void {
    if (!this.authService.isAuthenticated()) {
      this.authService.openAuthModal('login');
      return;
    }

    const t = this.title();
    if (!t) return;

    this.isSubmittingRating.set(true);
    this.catalogService.removeRating(t.id).subscribe({
      next: (updatedRating: RatingDto) => {
        this.ratingData.set(updatedRating);
        this.isSubmittingRating.set(false);
      },
      error: () => {
        this.isSubmittingRating.set(false);
      }
    });
  }

  openTrailer(video: VideoItem): void {
    this.activeTrailer.set(video);
  }

  closeTrailer(): void {
    this.activeTrailer.set(null);
  }

  getTrailerEmbedUrl(key: string): SafeResourceUrl {
    const url = `https://www.youtube-nocookie.com/embed/${key}?autoplay=1&rel=0`;
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }

  getProfileUrl(path: string | null): string | null {
    if (!path) return null;
    const cleanPath = path.startsWith('/') ? path : `/${path}`;
    return `${TMDB_IMAGE_BASE_URL}${TMDB_PROFILE_SIZE}${cleanPath}`;
  }

  getStillUrl(path: string | null): string | null {
    if (!path) return null;
    const cleanPath = path.startsWith('/') ? path : `/${path}`;
    return `${TMDB_IMAGE_BASE_URL}${TMDB_STILL_SIZE}${cleanPath}`;
  }

  handleImageError(event: Event, name: string): void {
    const img = event.target as HTMLImageElement;
    if (img) {
      img.style.display = 'none';
      const parent = img.parentElement;
      if (parent && !parent.querySelector('.avatar-placeholder')) {
        const placeholder = document.createElement('div');
        placeholder.className = 'avatar-placeholder';
        placeholder.textContent = this.getInitials(name);
        parent.appendChild(placeholder);
      }
    }
  }

  getInitials(name: string): string {
    if (!name) return '?';
    return name
      .split(' ')
      .map(part => part.charAt(0))
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  toggleWatchlist(): void {
    const t = this.title();
    if (!t) return;

    if (!this.authService.isAuthenticated()) {
      this.authService.openAuthModal('login');
      return;
    }

    this.userDataService.toggleWatchlist(t.id, {
      titleId: t.id,
      title: t.title,
      posterPath: t.posterPath,
      backdropPath: t.backdropPath,
      type: t.type,
      rating: t.rating
    }).subscribe();
  }

  goBack(): void {
    this.router.navigate(['/movies']);
  }
}
