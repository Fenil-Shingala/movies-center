import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AdminService } from './admin.service';

describe('AdminService', () => {
  let service: AdminService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AdminService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AdminService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should get stats overview', () => {
    const mockStats = {
      totalTitles: 100,
      totalMovies: 70,
      totalSeries: 30,
      totalSources: 200,
      pendingReports: 5,
      totalUsers: 25,
      lastSyncStatus: {
        isRunning: false,
        lastRunAt: '2026-10-26T00:00:00Z',
        lastStatus: 'Success',
        lastItemsSynced: 50,
        lastErrors: 0
      }
    };

    service.getStatsOverview().subscribe(res => {
      expect(res.totalTitles).toBe(100);
      expect(res.pendingReports).toBe(5);
    });

    const req = httpMock.expectOne(request => request.url.endsWith('/api/v1/admin/stats'));
    expect(req.request.method).toBe('GET');
    req.flush(mockStats);
  });

  it('should toggle title featured status', () => {
    service.toggleTitleFeatured(12).subscribe(res => {
      expect(res.message).toBe('Toggled');
    });

    const req = httpMock.expectOne(request => request.url.endsWith('/api/v1/admin/titles/12/toggle-featured'));
    expect(req.request.method).toBe('POST');
    req.flush({ message: 'Toggled' });
  });

  it('should search tmdb with query', () => {
    const mockResults = [
      {
        tmdbId: 101,
        title: 'Inception',
        type: 'movie',
        posterPath: '/p.jpg',
        backdropPath: '/b.jpg',
        releaseDate: '2010-07-16',
        year: '2010',
        rating: 8.8,
        overview: 'A thief steals corporate secrets...'
      }
    ];

    service.searchTmdb('Inception', 'movie').subscribe(res => {
      expect(res.length).toBe(1);
      expect(res[0].title).toBe('Inception');
    });

    const req = httpMock.expectOne(request => 
      request.url.endsWith('/api/v1/admin/tmdb/search') &&
      request.params.get('q') === 'Inception' &&
      request.params.get('type') === 'movie'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResults);
  });
});
