import { 
  Component, 
  OnInit, 
  OnDestroy, 
  signal, 
  computed, 
  inject, 
  HostListener 
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { Subscription, switchMap, catchError, of } from 'rxjs';
import { CatalogService } from '../../core/services/catalog.service';
import { TitleDetail, TitleSource, SeasonItem, EpisodeItem } from '../../core/models/media.models';
import { 
  LucidePlay, 
  LucideSun, 
  LucideMoon, 
  LucideArrowLeft, 
  LucideMaximize2, 
  LucideMinimize2, 
  LucideFlag, 
  LucideAlertCircle, 
  LucideAlertTriangle, 
  LucideRotateCw, 
  LucideServer, 
  LucideInfo, 
  LucideChevronLeft, 
  LucideChevronRight, 
  LucideCheckCircle, 
  LucideX, 
  LucideSend 
} from '@lucide/angular';

@Component({
  selector: 'app-player',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    FormsModule,
    LucidePlay, 
    LucideSun, 
    LucideMoon, 
    LucideArrowLeft, 
    LucideMaximize2, 
    LucideMinimize2, 
    LucideFlag, 
    LucideAlertCircle, 
    LucideAlertTriangle, 
    LucideRotateCw, 
    LucideServer, 
    LucideInfo, 
    LucideChevronLeft, 
    LucideChevronRight, 
    LucideCheckCircle, 
    LucideX, 
    LucideSend
  ],
  templateUrl: './player.component.html',
  styleUrls: ['./player.component.scss']
})
export class PlayerComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly catalogService = inject(CatalogService);
  private readonly sanitizer = inject(DomSanitizer);

  private sub?: Subscription;

  // Title & Source State
  readonly titleId = signal<number>(0);
  readonly mediaType = signal<'movie' | 'series'>('movie');
  readonly title = signal<TitleDetail | null>(null);
  readonly sources = signal<TitleSource[]>([]);
  readonly selectedSource = signal<TitleSource | null>(null);

  // Series Episode State
  readonly currentSeason = signal<number>(1);
  readonly currentEpisode = signal<number>(1);

  // View & UI Toggles
  readonly isLightsOff = signal<boolean>(false);
  readonly isTheaterMode = signal<boolean>(false);
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);

  // Report Modal State
  readonly showReportModal = signal<boolean>(false);
  readonly reportReason = signal<string>('playback');
  readonly reportDetails = signal<string>('');
  readonly isReporting = signal<boolean>(false);
  readonly reportSuccessMessage = signal<string | null>(null);
  readonly reportErrorMessage = signal<string | null>(null);

  // Computed Properties
  readonly activeSeasonObj = computed(() => {
    const item = this.title();
    if (!item || item.type !== 'series') return null;
    const seasonNum = this.currentSeason();
    return item.seasons.find(s => s.seasonNumber === seasonNum) ?? item.seasons[0] ?? null;
  });

  readonly currentEpisodeObj = computed(() => {
    const season = this.activeSeasonObj();
    if (!season) return null;
    const epNum = this.currentEpisode();
    return season.episodes.find(e => e.episodeNumber === epNum) ?? null;
  });

  readonly sanitizedUrl = computed<SafeResourceUrl | null>(() => {
    const src = this.selectedSource();
    if (!src || !src.url) return null;
    return this.sanitizer.bypassSecurityTrustResourceUrl(src.url);
  });

  readonly backLink = computed(() => {
    const item = this.title();
    if (!item) return '/';
    return `/${item.type}/${item.tmdbId}`;
  });

  readonly hasPreviousEpisode = computed(() => {
    const item = this.title();
    if (!item || item.type !== 'series') return false;
    const s = this.currentSeason();
    const e = this.currentEpisode();
    if (e > 1) return true;
    return s > 1;
  });

  readonly hasNextEpisode = computed(() => {
    const item = this.title();
    if (!item || item.type !== 'series') return false;
    const s = this.currentSeason();
    const e = this.currentEpisode();
    const currentSeason = this.activeSeasonObj();
    if (currentSeason && e < currentSeason.episodes.length) return true;
    const nextSeason = item.seasons.find(season => season.seasonNumber === s + 1);
    return !!nextSeason && nextSeason.episodes.length > 0;
  });

  ngOnInit(): void {
    this.sub = this.route.paramMap.subscribe(params => {
      const typeParam = params.get('type') as 'movie' | 'series' | null;
      const idParam = params.get('id');

      if (idParam) {
        const id = parseInt(idParam, 10);
        this.titleId.set(id);
        this.mediaType.set(typeParam === 'series' ? 'series' : 'movie');

        // Check query params for season & episode
        this.route.queryParamMap.subscribe(queryParams => {
          const sParam = queryParams.get('season');
          const eParam = queryParams.get('ep');

          const sNum = sParam ? parseInt(sParam, 10) : 1;
          const eNum = eParam ? parseInt(eParam, 10) : 1;

          this.currentSeason.set(isNaN(sNum) || sNum < 1 ? 1 : sNum);
          this.currentEpisode.set(isNaN(eNum) || eNum < 1 ? 1 : eNum);

          this.loadTitleAndSources();
        });
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.showReportModal()) {
      this.closeReportModal();
    } else if (this.isLightsOff()) {
      this.toggleLights();
    }
  }

  loadTitleAndSources(): void {
    const id = this.titleId();
    if (!id) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const isSeries = this.mediaType() === 'series';
    const s = isSeries ? this.currentSeason() : null;
    const e = isSeries ? this.currentEpisode() : null;

    // Load Title detail if not loaded yet
    if (!this.title() || this.title()?.tmdbId !== id) {
      this.catalogService.getTitleDetail(id).pipe(
        catchError(err => {
          console.error('Failed to load title details', err);
          return of(null);
        })
      ).subscribe(detail => {
        if (detail) {
          this.title.set(detail);
        }
      });
    }

    // Load Sources
    this.catalogService.getTitleSources(id, s, e).pipe(
      catchError(err => {
        console.error('Failed to load video sources', err);
        return of([] as TitleSource[]);
      })
    ).subscribe(sources => {
      this.isLoading.set(false);
      this.sources.set(sources);
      if (sources.length > 0) {
        // Default to highest priority source
        this.selectedSource.set(sources[0]);
      } else {
        this.selectedSource.set(null);
        this.errorMessage.set('No video sources are currently available for this title. Please check back later.');
      }
    });
  }

  selectSource(source: TitleSource): void {
    this.selectedSource.set(source);
  }

  selectSeason(seasonNumber: number): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { season: seasonNumber, ep: 1 },
      queryParamsHandling: 'merge'
    });
  }

  selectEpisode(episodeNumber: number): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { season: this.currentSeason(), ep: episodeNumber },
      queryParamsHandling: 'merge'
    });
  }

  goToPreviousEpisode(): void {
    const item = this.title();
    if (!item || item.type !== 'series') return;

    const s = this.currentSeason();
    const e = this.currentEpisode();

    if (e > 1) {
      this.selectEpisode(e - 1);
    } else if (s > 1) {
      const prevSeason = item.seasons.find(season => season.seasonNumber === s - 1);
      const targetEp = prevSeason?.episodes.length ? prevSeason.episodes.length : 1;
      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { season: s - 1, ep: targetEp },
        queryParamsHandling: 'merge'
      });
    }
  }

  goToNextEpisode(): void {
    const item = this.title();
    if (!item || item.type !== 'series') return;

    const s = this.currentSeason();
    const e = this.currentEpisode();
    const currentSeason = this.activeSeasonObj();

    if (currentSeason && e < currentSeason.episodes.length) {
      this.selectEpisode(e + 1);
    } else {
      const nextSeason = item.seasons.find(season => season.seasonNumber === s + 1);
      if (nextSeason) {
        this.router.navigate([], {
          relativeTo: this.route,
          queryParams: { season: s + 1, ep: 1 },
          queryParamsHandling: 'merge'
        });
      }
    }
  }

  toggleLights(): void {
    this.isLightsOff.update(val => !val);
  }

  toggleTheater(): void {
    this.isTheaterMode.update(val => !val);
  }

  openReportModal(): void {
    this.reportReason.set('playback');
    this.reportDetails.set('');
    this.reportSuccessMessage.set(null);
    this.reportErrorMessage.set(null);
    this.showReportModal.set(true);
  }

  closeReportModal(): void {
    this.showReportModal.set(false);
  }

  submitReport(): void {
    const src = this.selectedSource();
    if (!src) return;

    this.isReporting.set(true);
    this.reportSuccessMessage.set(null);
    this.reportErrorMessage.set(null);

    const isSeries = this.mediaType() === 'series';

    this.catalogService.reportSource(src.id, {
      titleId: this.titleId(),
      reason: this.reportReason(),
      details: this.reportDetails().trim() || null,
      season: isSeries ? this.currentSeason() : null,
      episode: isSeries ? this.currentEpisode() : null
    }).subscribe({
      next: (res) => {
        this.isReporting.set(false);
        this.reportSuccessMessage.set(res.message || 'Report submitted successfully. Thank you for helping us improve!');
        setTimeout(() => {
          this.closeReportModal();
        }, 2200);
      },
      error: (err) => {
        this.isReporting.set(false);
        this.reportErrorMessage.set(err?.error?.message || 'Failed to submit report. Please try again.');
      }
    });
  }

  getEpisodeThumb(path: string | null): string {
    return path ? `https://image.tmdb.org/t/p/w300${path}` : '/assets/images/placeholder-backdrop.jpg';
  }
}
