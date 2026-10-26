import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID } from '@angular/core';
import { GuestStorageService } from './guest-storage.service';
import { GuestHistoryItem } from '../models/auth.models';

describe('GuestStorageService', () => {
  let service: GuestStorageService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        GuestStorageService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });
    service = TestBed.inject(GuestStorageService);
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should be created and start with empty state', () => {
    expect(service).toBeTruthy();
    expect(service.watchlistIds()).toEqual([]);
    expect(service.historyItems()).toEqual([]);
    expect(service.hasGuestData()).toBe(false);
  });

  it('should toggle watchlist item correctly', () => {
    // Add
    const added = service.toggleWatchlist(42);
    expect(added).toBe(true);
    expect(service.isInWatchlist(42)).toBe(true);
    expect(service.watchlistIds()).toContain(42);
    expect(service.hasGuestData()).toBe(true);

    // Remove
    const removed = service.toggleWatchlist(42);
    expect(removed).toBe(false);
    expect(service.isInWatchlist(42)).toBe(false);
    expect(service.watchlistIds()).not.toContain(42);
    expect(service.hasGuestData()).toBe(false);
  });

  it('should save and retrieve watch progress', () => {
    const historyItem: GuestHistoryItem = {
      titleId: 101,
      episodeId: 5,
      seasonNumber: 1,
      episodeNumber: 3,
      progressSeconds: 600,
      durationSeconds: 1800
    };

    service.saveProgress(historyItem);
    expect(service.historyItems().length).toBe(1);

    const retrieved = service.getProgress(101, 5);
    expect(retrieved).toBeTruthy();
    expect(retrieved?.progressSeconds).toBe(600);
  });

  it('should clear guest data properly', () => {
    service.addToWatchlist(1);
    service.addToWatchlist(2);
    service.saveProgress({ titleId: 1, progressSeconds: 120 });

    expect(service.hasGuestData()).toBe(true);

    service.clearGuestData();

    expect(service.watchlistIds()).toEqual([]);
    expect(service.historyItems()).toEqual([]);
    expect(service.hasGuestData()).toBe(false);
  });
});
