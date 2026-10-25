import { Component, input, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideFilm, LucideTv } from '@lucide/angular';
import { MediaCard } from '../../../../core/models/media.models';
import { TMDB_IMAGE_BASE_URL, TMDB_POSTER_SIZE } from '../../../../core/constants/app.constants';

@Component({
  selector: 'app-media-card',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideFilm, LucideTv],
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
      return null;
    }
    return `${TMDB_IMAGE_BASE_URL}${TMDB_POSTER_SIZE}${data.posterPath}`;
  });

  readonly detailRoute = computed(() => {
    const data = this.item();
    return `/${data.type}/${data.tmdbId}`;
  });

  onImageError(): void {
    this.imageFailed.set(true);
  }
}
