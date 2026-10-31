import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PLATFORM_ID } from '@angular/core';
import { UserDataService } from './user-data.service';
import { AuthService } from './auth.service';
import { GuestStorageService } from './guest-storage.service';

describe('UserDataService', () => {
  let service: UserDataService;
  let httpTesting: HttpTestingController;
  let guestStorage: GuestStorageService;
  let authService: AuthService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        UserDataService,
        AuthService,
        GuestStorageService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });

    httpTesting = TestBed.inject(HttpTestingController);
    guestStorage = TestBed.inject(GuestStorageService);
    authService = TestBed.inject(AuthService);
    service = TestBed.inject(UserDataService);

    const refreshReq = httpTesting.match('/api/v1/auth/refresh');
    if (refreshReq.length > 0) {
      refreshReq[0].error(new ProgressEvent('error'), { status: 401 });
    }
  });

  afterEach(() => {
    httpTesting.verify();
    localStorage.clear();
  });

  it('should be created and start with empty states', () => {
    expect(service).toBeTruthy();
    expect(service.watchlist()).toEqual([]);
    expect(service.history()).toEqual([]);
    expect(service.watchlistCount()).toBe(0);
    expect(service.historyCount()).toBe(0);
  });

  it('should toggle watchlist in guest mode', () => {
    service.toggleWatchlist(101, {
      title: 'Inception',
      type: 'movie',
      year: '2010'
    }).subscribe(isIn => {
      expect(isIn).toBe(true);
      expect(service.isInWatchlist(101)).toBe(true);
      expect(service.watchlistCount()).toBe(1);
    });

    // Toggle off
    service.toggleWatchlist(101).subscribe(isIn => {
      expect(isIn).toBe(false);
      expect(service.isInWatchlist(101)).toBe(false);
      expect(service.watchlistCount()).toBe(0);
    });
  });

  it('should save progress and calculate percent in guest mode', () => {
    service.updateProgress(202, 1800, 7200, undefined, undefined, undefined, {
      title: 'Interstellar',
      type: 'movie',
      posterPath: '/interstellar.jpg'
    }).subscribe(item => {
      expect(item).toBeTruthy();
      expect(item?.progressPercent).toBe(25);
      expect(item?.isCompleted).toBe(false);
      expect(service.historyCount()).toBe(1);
      expect(service.continueWatching().length).toBe(1);
      expect(service.continueWatching()[0].title).toBe('Interstellar');
      expect(service.continueWatching()[0].posterPath).toBe('/interstellar.jpg');
    });
  });

  it('should automatically resolve missing metadata for guest history items', () => {
    // Add raw guest history item with missing metadata
    guestStorage.saveProgress({
      titleId: 303,
      progressSeconds: 500,
      durationSeconds: 2000
    });

    (service as any).loadGuestState();

    const req = httpTesting.expectOne('/api/v1/titles/303');
    expect(req.request.method).toBe('GET');
    req.flush({
      id: 303,
      tmdbId: 303,
      title: 'Resolved Title',
      posterPath: '/resolved.jpg',
      backdropPath: '/resolved-bg.jpg',
      type: 'movie',
      rating: 8.5,
      year: '2024'
    });

    const items = service.history();
    const item = items.find(i => i.titleId === 303);
    expect(item?.title).toBe('Resolved Title');
    expect(item?.posterPath).toBe('/resolved.jpg');
  });

  it('should clear history cleanly', () => {
    service.updateProgress(202, 1800, 7200, undefined, undefined, undefined, {
      title: 'Interstellar',
      type: 'movie',
      posterPath: '/interstellar.jpg'
    }).subscribe();
    expect(service.historyCount()).toBe(1);

    service.clearHistory().subscribe(success => {
      expect(success).toBe(true);
      expect(service.historyCount()).toBe(0);
      expect(service.continueWatching().length).toBe(0);
    });
  });
});
