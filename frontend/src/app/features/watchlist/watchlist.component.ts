import { Component, OnInit, signal, computed, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { 
  LucideBookmark, 
  LucidePlay, 
  LucideTrash2, 
  LucideStar, 
  LucideFilm, 
  LucideSparkles 
} from '@lucide/angular';
import { UserDataService } from '../../core/services/user-data.service';
import { AuthService } from '../../core/services/auth.service';
import { WatchlistItem } from '../../core/models/media.models';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-watchlist',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    LucideBookmark,
    LucidePlay,
    LucideTrash2,
    LucideStar,
    LucideFilm,
    LucideSparkles,
    EmptyStateComponent
  ],
  templateUrl: './watchlist.component.html',
  styleUrl: './watchlist.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WatchlistComponent implements OnInit {
  readonly userDataService = inject(UserDataService);
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly titleService = inject(Title);

  readonly selectedFilter = signal<'all' | 'movie' | 'series'>('all');

  readonly filteredItems = computed(() => {
    const list = this.userDataService.watchlist();
    const filter = this.selectedFilter();
    if (filter === 'all') return list;
    return list.filter(item => item.type === filter);
  });

  ngOnInit(): void {
    this.titleService.setTitle('My Watchlist - Movies Center');
    this.userDataService.loadWatchlist().subscribe();
  }

  setFilter(filter: 'all' | 'movie' | 'series'): void {
    this.selectedFilter.set(filter);
  }

  removeItem(event: Event, item: WatchlistItem): void {
    event.stopPropagation();
    event.preventDefault();
    this.userDataService.removeFromWatchlist(item.titleId).subscribe();
  }

  getPosterUrl(path: string | null): string {
    return path ? `https://image.tmdb.org/t/p/w342${path}` : '';
  }

  getWatchLink(item: WatchlistItem): string {
    return `/watch/${item.type}/${item.titleId}`;
  }

  getDetailLink(item: WatchlistItem): string {
    return `/title/${item.titleId}`;
  }

  goToCatalog(): void {
    this.router.navigate(['/movies']);
  }
}
