import { 
  Component, 
  OnInit, 
  inject, 
  signal, 
  computed, 
  HostListener, 
  ChangeDetectionStrategy 
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
  LucideX 
} from '@lucide/angular';
import { CatalogService } from '../../core/services/catalog.service';
import { UserDataService } from '../../core/services/user-data.service';
import { 
  TitleDetail, 
  SeasonItem, 
  VideoItem,
  EpisodeItem 
} from '../../core/models/media.models';
import { 
  TMDB_IMAGE_BASE_URL, 
  TMDB_POSTER_SIZE, 
  TMDB_BACKDROP_SIZE 
} from '../../core/constants/app.constants';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { CustomSelectComponent, SelectOption } from '../../shared/components/custom-select/custom-select.component';

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
    EmptyStateComponent
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

  readonly title = signal<TitleDetail | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly selectedSeasonNumber = signal<number>(1);
  readonly activeTrailer = signal<VideoItem | null>(null);
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

  readonly formattedRuntime = computed(() => {
    const item = this.title();
    if (!item) return null;

    if (item.type === 'series') {
      const s = item.totalSeasons ?? item.seasons.length;
      return s === 1 ? '1 Season' : `${s} Seasons`;
    }

    if (item.runtimeMinutes) {
      const h = Math.floor(item.runtimeMinutes / 60);
      const m = item.runtimeMinutes % 60;
      return h > 0 ? `${h}h ${m}m` : `${m}m`;
    }

    return null;
  });

  readonly mainTrailer = computed(() => {
    const item = this.title();
    if (!item || item.videos.length === 0) return null;
    return item.videos.find(v => v.type === 'Trailer' && v.site === 'YouTube') 
      ?? item.videos.find(v => v.site === 'YouTube') 
      ?? item.videos[0];
  });

  readonly watchLink = computed(() => {
    const item = this.title();
    if (!item) return '/';
    return `/watch/${item.type}/${item.tmdbId}`;
  });

  readonly seasonOptions = computed<SelectOption<number>[]>(() => {
    const item = this.title();
    if (!item || item.type !== 'series') return [];
    return item.seasons.map(s => ({
      value: s.seasonNumber,
      label: s.name || `Season ${s.seasonNumber}`,
      badge: `${s.episodeCount || s.episodes.length} Ep`,
      subtext: s.airDate ? `Premiered ${s.airDate}` : undefined
    }));
  });

  readonly activeSeason = computed(() => {
    const item = this.title();
    if (!item || item.seasons.length === 0) return null;
    const num = this.selectedSeasonNumber();
    const season = item.seasons.find(s => s.seasonNumber === num) ?? item.seasons[0];

    // Ensure episodes array is never empty if episodeCount is known
    if (season.episodes.length === 0 && (season.episodeCount > 0 || season.seasonNumber > 0)) {
      const count = season.episodeCount > 0 ? season.episodeCount : 10;
      const fallbackEpisodes: EpisodeItem[] = Array.from({ length: count }, (_, i) => ({
        id: i + 1,
        tmdbId: season.tmdbId * 100 + (i + 1),
        episodeNumber: i + 1,
        name: `Episode ${i + 1}`,
        overview: null,
        stillPath: season.posterPath,
        airDate: season.airDate,
        runtimeMinutes: null
      }));
      return { ...season, episodes: fallbackEpisodes };
    }

    return season;
  });

  onSeasonSelect(seasonNumber: number): void {
    this.selectedSeasonNumber.set(seasonNumber);
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      if (idParam) {
        const id = parseInt(idParam, 10);
        if (!isNaN(id) && id > 0) {
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
      },
      error: () => {
        this.title.set(null);
        this.isLoading.set(false);
      }
    });
  }

  onSeasonChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const num = parseInt(select.value, 10);
    this.selectedSeasonNumber.set(num);
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

  getProviderLogoUrl(path: string): string {
    return `https://image.tmdb.org/t/p/w92${path}`;
  }

  getProfileUrl(path: string): string {
    return `https://image.tmdb.org/t/p/w185${path}`;
  }

  getStillUrl(path: string): string {
    return `https://image.tmdb.org/t/p/w300${path}`;
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .slice(0, 2)
      .map(part => part.charAt(0).toUpperCase())
      .join('');
  }

  toggleWatchlist(): void {
    const t = this.title();
    if (t) {
      this.userDataService.toggleWatchlist(t.id, {
        title: t.title,
        posterPath: t.posterPath,
        backdropPath: t.backdropPath,
        type: t.type,
        releaseDate: t.releaseDate,
        year: t.year,
        rating: t.rating
      }).subscribe();
    }
  }

  goBack(): void {
    this.router.navigate(['/movies']);
  }
}
