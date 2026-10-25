import { Component, input, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucidePlay, LucideInfo } from '@lucide/angular';
import { HeroItem } from '../../../../core/models/media.models';
import { TMDB_IMAGE_BASE_URL, TMDB_BACKDROP_SIZE } from '../../../../core/constants/app.constants';

@Component({
  selector: 'app-hero-banner',
  standalone: true,
  imports: [CommonModule, RouterLink, LucidePlay, LucideInfo],
  templateUrl: './hero-banner.component.html',
  styleUrl: './hero-banner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeroBannerComponent {
  readonly hero = input.required<HeroItem>();

  readonly backdropUrl = computed(() => {
    const item = this.hero();
    if (!item?.backdropPath) {
      return null;
    }
    return `${TMDB_IMAGE_BASE_URL}${TMDB_BACKDROP_SIZE}${item.backdropPath}`;
  });

  readonly formattedRuntime = computed(() => {
    const item = this.hero();
    if (item.totalSeasons) {
      return item.totalSeasons === 1 ? '1 Season' : `${item.totalSeasons} Seasons`;
    }
    if (item.runtimeMinutes) {
      const hours = Math.floor(item.runtimeMinutes / 60);
      const mins = item.runtimeMinutes % 60;
      return hours > 0 ? `${hours}h ${mins}m` : `${mins}m`;
    }
    return null;
  });

  readonly watchLink = computed(() => {
    const item = this.hero();
    return `/watch/${item.type}/${item.tmdbId}`;
  });

  readonly detailsLink = computed(() => {
    const item = this.hero();
    return `/${item.type}/${item.tmdbId}`;
  });
}
