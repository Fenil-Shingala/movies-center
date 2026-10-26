import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { 
  LucideClock, 
  LucidePlay, 
  LucideTrash2, 
  LucideCheck, 
  LucideFilm, 
  LucideSparkles 
} from '@lucide/angular';
import { UserDataService } from '../../core/services/user-data.service';
import { AuthService } from '../../core/services/auth.service';
import { WatchHistoryItem } from '../../core/models/media.models';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    LucideClock,
    LucidePlay,
    LucideTrash2,
    LucideCheck,
    LucideFilm,
    LucideSparkles,
    EmptyStateComponent
  ],
  templateUrl: './history.component.html',
  styleUrl: './history.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HistoryComponent implements OnInit {
  readonly userDataService = inject(UserDataService);
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly titleService = inject(Title);

  ngOnInit(): void {
    this.titleService.setTitle('Watch History - Movies Center');
    this.userDataService.loadHistory().subscribe();
  }

  removeItem(event: Event, item: WatchHistoryItem): void {
    event.stopPropagation();
    event.preventDefault();
    this.userDataService.removeFromHistory(item.id, item.titleId).subscribe();
  }

  clearAll(): void {
    if (confirm('Are you sure you want to clear your entire watch history?')) {
      this.userDataService.clearHistory().subscribe();
    }
  }

  getThumbnailUrl(item: WatchHistoryItem): string {
    const path = item.backdropPath || item.posterPath;
    return path ? `https://image.tmdb.org/t/p/w300${path}` : '';
  }

  getWatchLink(item: WatchHistoryItem): string {
    if (item.type === 'series') {
      const s = item.seasonNumber || 1;
      const e = item.episodeNumber || 1;
      return `/watch/series/${item.titleId}?season=${s}&ep=${e}`;
    }
    return `/watch/movie/${item.titleId}`;
  }

  formatTime(seconds: number): string {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s < 10 ? '0' : ''}${s}`;
  }

  goToCatalog(): void {
    this.router.navigate(['/']);
  }
}
