import { Injectable, signal, computed, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { GuestData, GuestHistoryItem } from '../models/auth.models';

const WATCHLIST_KEY = 'mc_guest_watchlist';
const HISTORY_KEY = 'mc_guest_history';

@Injectable({
  providedIn: 'root'
})
export class GuestStorageService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  readonly watchlistIds = signal<number[]>([]);
  readonly historyItems = signal<GuestHistoryItem[]>([]);

  readonly hasGuestData = computed(() => 
    this.watchlistIds().length > 0 || this.historyItems().length > 0
  );

  readonly guestWatchlistCount = computed(() => this.watchlistIds().length);
  readonly guestHistoryCount = computed(() => this.historyItems().length);

  constructor() {
    this.loadFromStorage();
  }

  private loadFromStorage(): void {
    if (!this.isBrowser) return;

    try {
      const rawWatchlist = localStorage.getItem(WATCHLIST_KEY);
      if (rawWatchlist) {
        const parsed = JSON.parse(rawWatchlist);
        if (Array.isArray(parsed)) {
          this.watchlistIds.set(parsed.filter(id => typeof id === 'number'));
        }
      }

      const rawHistory = localStorage.getItem(HISTORY_KEY);
      if (rawHistory) {
        const parsed = JSON.parse(rawHistory);
        if (Array.isArray(parsed)) {
          this.historyItems.set(parsed);
        }
      }
    } catch (e) {
      console.warn('Failed to read guest storage from localStorage', e);
    }
  }

  isInWatchlist(titleId: number): boolean {
    return this.watchlistIds().includes(titleId);
  }

  toggleWatchlist(titleId: number): boolean {
    const current = this.watchlistIds();
    const index = current.indexOf(titleId);
    let updated: number[];
    let inWatchlist: boolean;

    if (index >= 0) {
      updated = current.filter(id => id !== titleId);
      inWatchlist = false;
    } else {
      updated = [...current, titleId];
      inWatchlist = true;
    }

    this.watchlistIds.set(updated);
    this.persistWatchlist(updated);
    return inWatchlist;
  }

  addToWatchlist(titleId: number): void {
    if (!this.isInWatchlist(titleId)) {
      const updated = [...this.watchlistIds(), titleId];
      this.watchlistIds.set(updated);
      this.persistWatchlist(updated);
    }
  }

  removeFromWatchlist(titleId: number): void {
    if (this.isInWatchlist(titleId)) {
      const updated = this.watchlistIds().filter(id => id !== titleId);
      this.watchlistIds.set(updated);
      this.persistWatchlist(updated);
    }
  }

  saveProgress(item: GuestHistoryItem): void {
    const current = [...this.historyItems()];
    const index = current.findIndex(h => 
      h.titleId === item.titleId && 
      (h.episodeId ?? null) === (item.episodeId ?? null)
    );

    const updatedItem: GuestHistoryItem = {
      ...item,
      watchedAt: item.watchedAt ?? new Date().toISOString()
    };

    if (index >= 0) {
      current[index] = {
        ...current[index],
        ...updatedItem,
        title: updatedItem.title || current[index].title,
        posterPath: updatedItem.posterPath || current[index].posterPath,
        backdropPath: updatedItem.backdropPath || current[index].backdropPath,
        type: updatedItem.type || current[index].type,
        episodeName: updatedItem.episodeName || current[index].episodeName
      };
    } else {
      current.unshift(updatedItem);
    }

    // Keep history capped at 100 items for storage sanity
    const capped = current.slice(0, 100);
    this.historyItems.set(capped);
    this.persistHistory(capped);
  }

  updateItemMetadata(titleId: number, metadata: Partial<GuestHistoryItem>): void {
    const current = [...this.historyItems()];
    let changed = false;

    for (let i = 0; i < current.length; i++) {
      if (current[i].titleId === titleId) {
        current[i] = {
          ...current[i],
          title: metadata.title ?? current[i].title,
          posterPath: metadata.posterPath ?? current[i].posterPath,
          backdropPath: metadata.backdropPath ?? current[i].backdropPath,
          type: metadata.type ?? current[i].type,
          episodeName: metadata.episodeName ?? current[i].episodeName
        };
        changed = true;
      }
    }

    if (changed) {
      this.historyItems.set(current);
      this.persistHistory(current);
    }
  }

  getProgress(titleId: number, episodeId?: number | null): GuestHistoryItem | null {
    return this.historyItems().find(h => 
      h.titleId === titleId && 
      (h.episodeId ?? null) === (episodeId ?? null)
    ) ?? null;
  }

  getGuestData(): GuestData {
    return {
      watchlistTitleIds: this.watchlistIds(),
      history: this.historyItems()
    };
  }

  clearGuestData(): void {
    this.watchlistIds.set([]);
    this.historyItems.set([]);
    if (this.isBrowser) {
      try {
        localStorage.removeItem(WATCHLIST_KEY);
        localStorage.removeItem(HISTORY_KEY);
      } catch (e) {
        console.warn('Failed to clear guest storage', e);
      }
    }
  }

  private persistWatchlist(ids: number[]): void {
    if (!this.isBrowser) return;
    try {
      localStorage.setItem(WATCHLIST_KEY, JSON.stringify(ids));
    } catch (e) {
      console.warn('Failed to persist guest watchlist', e);
    }
  }

  private persistHistory(items: GuestHistoryItem[]): void {
    if (!this.isBrowser) return;
    try {
      localStorage.setItem(HISTORY_KEY, JSON.stringify(items));
    } catch (e) {
      console.warn('Failed to persist guest history', e);
    }
  }
}
