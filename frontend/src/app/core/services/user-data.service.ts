import { Injectable, signal, computed, inject, PLATFORM_ID, effect, untracked } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { Observable, of, tap, map, catchError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { WatchlistItem, WatchHistoryItem, PagedResult } from '../models/media.models';
import { AuthService } from './auth.service';
import { GuestStorageService } from './guest-storage.service';

@Injectable({
  providedIn: 'root'
})
export class UserDataService {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly guestStorage = inject(GuestStorageService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  private readonly apiUrl = `${environment.apiUrl}/v1`;

  readonly watchlist = signal<WatchlistItem[]>([]);
  readonly history = signal<WatchHistoryItem[]>([]);
  readonly isLoading = signal<boolean>(false);

  readonly watchlistCount = computed(() => this.watchlist().length);
  readonly historyCount = computed(() => this.history().length);
  readonly continueWatching = computed(() => 
    this.history().filter(h => !h.isCompleted && h.progressSeconds > 10)
  );

  constructor() {
    if (this.isBrowser) {
      // Re-load data when authentication state changes without tracking signals modified in loadGuestState/loadWatchlist
      effect(() => {
        const isAuth = this.authService.isAuthenticated();
        untracked(() => {
          if (isAuth) {
            this.loadWatchlist().subscribe();
            this.loadHistory().subscribe();
          } else {
            this.loadGuestState();
          }
        });
      });
    }
  }

  private loadGuestState(): void {
    const rawWatchlist = this.guestStorage.watchlistIds();
    // Maintain existing items or map from guest storage
    const currentList = this.watchlist();
    const filtered = currentList.filter(item => rawWatchlist.includes(item.titleId));
    this.watchlist.set(filtered);

    const guestHist = this.guestStorage.historyItems().map((g, index) => ({
      id: index + 1,
      titleId: g.titleId,
      title: 'Loading...',
      posterPath: null,
      backdropPath: null,
      type: (g.episodeId ? 'series' : 'movie') as 'movie' | 'series',
      episodeId: g.episodeId,
      seasonNumber: g.seasonNumber,
      episodeNumber: g.episodeNumber,
      episodeName: g.episodeNumber ? `Episode ${g.episodeNumber}` : null,
      progressSeconds: g.progressSeconds,
      durationSeconds: g.durationSeconds,
      progressPercent: (g.durationSeconds && g.durationSeconds > 0)
        ? Math.min(100, Math.round((g.progressSeconds / g.durationSeconds) * 100))
        : 0,
      isCompleted: !!(g.durationSeconds && g.durationSeconds > 0 && g.progressSeconds >= g.durationSeconds * 0.9),
      watchedAt: g.watchedAt ?? new Date().toISOString()
    }));
    this.history.set(guestHist);
  }

  loadWatchlist(type?: string): Observable<WatchlistItem[]> {
    if (!this.isBrowser) return of([]);

    if (this.authService.isAuthenticated()) {
      let params = new HttpParams().set('pageSize', '100');
      if (type) params = params.set('type', type);

      this.isLoading.set(true);
      return this.http.get<PagedResult<WatchlistItem>>(`${this.apiUrl}/me/watchlist`, { params }).pipe(
        map(res => res.items),
        tap(items => {
          this.watchlist.set(items);
          this.isLoading.set(false);
        }),
        catchError(() => {
          this.isLoading.set(false);
          return of([]);
        })
      );
    } else {
      // For guest, return current filtered by type if requested
      const items = type ? this.watchlist().filter(w => w.type === type) : this.watchlist();
      return of(items);
    }
  }

  isInWatchlist(titleId: number): boolean {
    if (this.authService.isAuthenticated()) {
      return this.watchlist().some(w => w.titleId === titleId);
    }
    return this.guestStorage.isInWatchlist(titleId);
  }

  toggleWatchlist(titleId: number, mediaFallback?: Partial<WatchlistItem>): Observable<boolean> {
    const currentlyIn = this.isInWatchlist(titleId);

    if (this.authService.isAuthenticated()) {
      if (currentlyIn) {
        return this.http.delete<{ success: boolean }>(`${this.apiUrl}/me/watchlist/${titleId}`).pipe(
          tap(() => {
            this.watchlist.update(items => items.filter(w => w.titleId !== titleId));
          }),
          map(() => false),
          catchError(() => of(true))
        );
      } else {
        return this.http.post<{ message: string }>(`${this.apiUrl}/me/watchlist`, { titleId }).pipe(
          tap(() => {
            if (mediaFallback) {
              const newItem: WatchlistItem = {
                id: Date.now(),
                titleId,
                title: mediaFallback.title ?? 'Unknown',
                posterPath: mediaFallback.posterPath ?? null,
                backdropPath: mediaFallback.backdropPath ?? null,
                type: mediaFallback.type ?? 'movie',
                releaseDate: mediaFallback.releaseDate ?? null,
                year: mediaFallback.year ?? '',
                rating: mediaFallback.rating ?? 0,
                addedAt: new Date().toISOString()
              };
              this.watchlist.update(items => [newItem, ...items.filter(w => w.titleId !== titleId)]);
            }
          }),
          map(() => true),
          catchError(() => of(false))
        );
      }
    } else {
      // Guest mode
      const nowIn = this.guestStorage.toggleWatchlist(titleId);
      if (nowIn && mediaFallback) {
        const newItem: WatchlistItem = {
          id: Date.now(),
          titleId,
          title: mediaFallback.title ?? 'Unknown',
          posterPath: mediaFallback.posterPath ?? null,
          backdropPath: mediaFallback.backdropPath ?? null,
          type: mediaFallback.type ?? 'movie',
          releaseDate: mediaFallback.releaseDate ?? null,
          year: mediaFallback.year ?? '',
          rating: mediaFallback.rating ?? 0,
          addedAt: new Date().toISOString()
        };
        this.watchlist.update(items => [newItem, ...items.filter(w => w.titleId !== titleId)]);
      } else if (!nowIn) {
        this.watchlist.update(items => items.filter(w => w.titleId !== titleId));
      }
      return of(nowIn);
    }
  }

  removeFromWatchlist(titleId: number): Observable<boolean> {
    if (this.authService.isAuthenticated()) {
      return this.http.delete<{ success: boolean }>(`${this.apiUrl}/me/watchlist/${titleId}`).pipe(
        tap(() => {
          this.watchlist.update(items => items.filter(w => w.titleId !== titleId));
        }),
        map(res => res.success ?? true),
        catchError(() => of(false))
      );
    } else {
      this.guestStorage.removeFromWatchlist(titleId);
      this.watchlist.update(items => items.filter(w => w.titleId !== titleId));
      return of(true);
    }
  }

  loadHistory(): Observable<WatchHistoryItem[]> {
    if (!this.isBrowser) return of([]);

    if (this.authService.isAuthenticated()) {
      this.isLoading.set(true);
      return this.http.get<PagedResult<WatchHistoryItem>>(`${this.apiUrl}/me/history`, {
        params: new HttpParams().set('pageSize', '50')
      }).pipe(
        map(res => res.items),
        tap(items => {
          this.history.set(items);
          this.isLoading.set(false);
        }),
        catchError(() => {
          this.isLoading.set(false);
          return of([]);
        })
      );
    } else {
      this.loadGuestState();
      return of(this.history());
    }
  }

  updateProgress(
    titleId: number,
    progressSeconds: number,
    durationSeconds?: number,
    episodeId?: number,
    seasonNumber?: number,
    episodeNumber?: number,
    mediaFallback?: Partial<WatchHistoryItem>
  ): Observable<WatchHistoryItem | null> {
    const isCompleted = !!(durationSeconds && durationSeconds > 0 && progressSeconds >= durationSeconds * 0.9);
    const percent = (durationSeconds && durationSeconds > 0)
      ? Math.min(100, Math.round((progressSeconds / durationSeconds) * 100))
      : 0;

    if (this.authService.isAuthenticated()) {
      const payload = {
        titleId,
        episodeId: episodeId ?? null,
        seasonNumber: seasonNumber ?? null,
        episodeNumber: episodeNumber ?? null,
        progressSeconds,
        durationSeconds: durationSeconds ?? null
      };

      return this.http.put<WatchHistoryItem>(`${this.apiUrl}/me/history`, payload).pipe(
        tap(updated => {
          this.history.update(current => {
            const index = current.findIndex(h => 
              h.titleId === titleId && (h.episodeId ?? null) === (episodeId ?? null)
            );
            if (index >= 0) {
              const copy = [...current];
              copy[index] = updated;
              return copy;
            }
            return [updated, ...current];
          });
        }),
        catchError(() => of(null))
      );
    } else {
      // Guest mode
      this.guestStorage.saveProgress({
        titleId,
        episodeId,
        seasonNumber,
        episodeNumber,
        progressSeconds,
        durationSeconds
      });

      const updatedItem: WatchHistoryItem = {
        id: Date.now(),
        titleId,
        title: mediaFallback?.title ?? 'Unknown',
        posterPath: mediaFallback?.posterPath ?? null,
        backdropPath: mediaFallback?.backdropPath ?? null,
        type: mediaFallback?.type ?? (episodeId ? 'series' : 'movie'),
        episodeId: episodeId ?? null,
        seasonNumber: seasonNumber ?? null,
        episodeNumber: episodeNumber ?? null,
        episodeName: episodeNumber ? `Episode ${episodeNumber}` : null,
        progressSeconds,
        durationSeconds: durationSeconds ?? null,
        progressPercent: percent,
        isCompleted,
        watchedAt: new Date().toISOString()
      };

      this.history.update(current => {
        const index = current.findIndex(h => 
          h.titleId === titleId && (h.episodeId ?? null) === (episodeId ?? null)
        );
        if (index >= 0) {
          const copy = [...current];
          copy[index] = updatedItem;
          return copy;
        }
        return [updatedItem, ...current];
      });

      return of(updatedItem);
    }
  }

  removeFromHistory(historyId: number, titleId?: number): Observable<boolean> {
    if (this.authService.isAuthenticated()) {
      return this.http.delete<{ success: boolean }>(`${this.apiUrl}/me/history/${historyId}`).pipe(
        tap(() => {
          this.history.update(items => items.filter(h => h.id !== historyId));
        }),
        map(res => res.success ?? true),
        catchError(() => of(false))
      );
    } else {
      // Guest mode: remove by titleId if provided, or historyId
      this.history.update(items => items.filter(h => h.id !== historyId && (!titleId || h.titleId !== titleId)));
      return of(true);
    }
  }

  clearHistory(): Observable<boolean> {
    if (this.authService.isAuthenticated()) {
      return this.http.delete<{ message: string }>(`${this.apiUrl}/me/history`).pipe(
        tap(() => this.history.set([])),
        map(() => true),
        catchError(() => of(false))
      );
    } else {
      this.guestStorage.clearGuestData();
      this.history.set([]);
      return of(true);
    }
  }
}
