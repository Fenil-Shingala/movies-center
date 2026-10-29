import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HomeService } from './home.service';
import { HomeFeed } from '../models/media.models';

describe('HomeService', () => {
  let service: HomeService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        HomeService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(HomeService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should get home feed from /api/v1/home', () => {
    const mockFeed: HomeFeed = {
      hero: {
        tmdbId: 101,
        type: 'movie',
        title: 'Hero Title',
        overview: 'Overview',
        backdropPath: '/b.jpg',
        posterPath: '/p.jpg',
        rating: 8.5,
        releaseDate: '2024-01-01',
        year: '2024',
        runtimeMinutes: 120,
        totalSeasons: null,
        genres: ['Action'],
        quality: '4K Ultra HD'
      },
      rows: []
    };

    service.getHomeFeed().subscribe(data => {
      expect(data).toEqual(mockFeed);
      expect(data.hero.title).toBe('Hero Title');
    });

    const req = httpMock.expectOne('/api/v1/home');
    expect(req.request.method).toBe('GET');
    req.flush(mockFeed);
  });

  it('should get top10 ranked titles from /api/v1/home/top10', () => {
    const mockTop10 = [
      {
        tmdbId: 101,
        type: 'movie' as const,
        title: 'Top Movie',
        posterPath: '/p.jpg',
        backdropPath: '/b.jpg',
        rating: 9.1,
        year: '2026',
        genres: ['Action']
      }
    ];

    service.getTop10('movie').subscribe(data => {
      expect(data).toHaveLength(1);
      expect(data[0].title).toBe('Top Movie');
    });

    const req = httpMock.expectOne(r => r.url === '/api/v1/home/top10' && r.params.get('type') === 'movie');
    expect(req.request.method).toBe('GET');
    req.flush(mockTop10);
  });
});
