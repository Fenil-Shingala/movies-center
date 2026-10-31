import { Component, input, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MediaCard } from '../../../../core/models/media.models';
import { TMDB_IMAGE_BASE_URL, TMDB_POSTER_SIZE, DEFAULT_POSTER_IMAGE } from '../../../../core/constants/app.constants';

@Component({
  selector: 'app-media-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './media-card.component.html',
  styleUrl: './media-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MediaCardComponent {
  readonly item = input.required<MediaCard>();
  readonly imageFailed = signal(false);

  readonly posterUrl = computed(() => {
    const data = this.item();
    if (this.imageFailed() || !data.posterPath) {
      return DEFAULT_POSTER_IMAGE;
    }
    const path = data.posterPath;
    const cleanPath = path.startsWith('/') ? path : `/${path}`;
    return `${TMDB_IMAGE_BASE_URL}${TMDB_POSTER_SIZE}${cleanPath}`;
  });

  readonly detailRoute = computed(() => {
    const data = this.item();
    return `/${data.type}/${data.tmdbId}`;
  });

  onImageError(event?: Event): void {
    if (!this.imageFailed()) {
      this.imageFailed.set(true);
    }
    if (event?.target) {
      const img = event.target as HTMLImageElement;
      if (!img.src.includes('default-poster.svg')) {
        img.src = DEFAULT_POSTER_IMAGE;
      }
    }
  }
}
