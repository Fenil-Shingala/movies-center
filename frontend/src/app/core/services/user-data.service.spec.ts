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
      type: 'movie'
    }).subscribe(item => {
      expect(item).toBeTruthy();
      expect(item?.progressPercent).toBe(25);
      expect(item?.isCompleted).toBe(false);
      expect(service.historyCount()).toBe(1);
      expect(service.continueWatching().length).toBe(1);
    });
  });

  it('should clear history cleanly', () => {
    service.updateProgress(202, 1800, 7200).subscribe();
    expect(service.historyCount()).toBe(1);

    service.clearHistory().subscribe(success => {
      expect(success).toBe(true);
      expect(service.historyCount()).toBe(0);
      expect(service.continueWatching().length).toBe(0);
    });
  });
});
