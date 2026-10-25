import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CatalogService } from './catalog.service';
import { PagedResult, MediaCard, GenreItem, SearchSuggestion } from '../models/media.models';

describe('CatalogService', () => {
  let service: CatalogService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        CatalogService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(CatalogService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should browse titles with query params', () => {
    const mockPagedResult: PagedResult<MediaCard> = {
      items: [
        {
          tmdbId: 101,
          type: 'movie',
          title: 'Inception',
          backdropPath: '/b.jpg',
          posterPath: '/p.jpg',
          rating: 8.8,
          year: '2010',
          genres: ['Action', 'Sci-Fi']
        }
      ],
      page: 1,
      pageSize: 24,
      totalCount: 1,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false
    };

    service.browseTitles('movie', { genre: 28, sort: 'rating.desc', page: 1, pageSize: 24 }).subscribe((res: PagedResult<MediaCard>) => {
      expect(res.items.length).toBe(1);
      expect(res.items[0].title).toBe('Inception');
      expect(res.totalCount).toBe(1);
    });

    const req = httpMock.expectOne(request => 
      request.url.endsWith('/api/v1/titles') &&
      request.params.get('type') === 'movie' &&
      request.params.get('genre') === '28' &&
      request.params.get('sort') === 'rating.desc'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockPagedResult);
  });

  it('should search titles with query and type', () => {
    const mockPagedResult: PagedResult<MediaCard> = {
      items: [
        {
          tmdbId: 155,
          type: 'movie',
          title: 'The Dark Knight',
          backdropPath: '/b.jpg',
          posterPath: '/p.jpg',
          rating: 9.0,
          year: '2008',
          genres: ['Action', 'Crime']
        }
      ],
      page: 1,
      pageSize: 24,
      totalCount: 1,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false
    };

    service.searchTitles('dark', 'movie', 1, 24).subscribe(res => {
      expect(res.items.length).toBe(1);
      expect(res.items[0].title).toBe('The Dark Knight');
    });

    const req = httpMock.expectOne(request => 
      request.url.endsWith('/api/v1/search') &&
      request.params.get('q') === 'dark' &&
      request.params.get('type') === 'movie'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockPagedResult);
  });

  it('should return empty list if suggestion query is too short', () => {
    service.getSearchSuggestions('a').subscribe(suggestions => {
      expect(suggestions).toEqual([]);
    });

    httpMock.expectNone(request => request.url.includes('/api/v1/search/suggest'));
  });

  it('should fetch search suggestions when query is valid', () => {
    const mockSuggestions: SearchSuggestion[] = [
      {
        tmdbId: 155,
        type: 'movie',
        title: 'The Dark Knight',
        posterPath: '/p.jpg',
        rating: 9.0,
        year: '2008',
        genres: ['Action', 'Crime']
      }
    ];

    service.getSearchSuggestions('dark', 6).subscribe(suggestions => {
      expect(suggestions.length).toBe(1);
      expect(suggestions[0].title).toBe('The Dark Knight');
    });

    const req = httpMock.expectOne(request => 
      request.url.endsWith('/api/v1/search/suggest') &&
      request.params.get('q') === 'dark' &&
      request.params.get('limit') === '6'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockSuggestions);
  });

  it('should fetch genres from /api/v1/genres and cache result', () => {
    const mockGenres: GenreItem[] = [
      { id: 28, name: 'Action', slug: 'action' },
      { id: 35, name: 'Comedy', slug: 'comedy' }
    ];

    service.getGenres().subscribe((genres: GenreItem[]) => {
      expect(genres.length).toBe(2);
      expect(genres[0].name).toBe('Action');
    });

    const req = httpMock.expectOne(request => request.url.endsWith('/api/v1/genres'));
    expect(req.request.method).toBe('GET');
    req.flush(mockGenres);

    // Call again to verify cached result (no new http request)
    service.getGenres().subscribe((genres: GenreItem[]) => {
      expect(genres.length).toBe(2);
    });
  });

  it('should fetch title details by id', () => {
    const mockDetail = {
      id: 1,
      tmdbId: 155,
      type: 'movie' as const,
      title: 'The Dark Knight',
      rating: 9.0
    } as any;

    service.getTitleDetail(155).subscribe(res => {
      expect(res.title).toBe('The Dark Knight');
      expect(res.tmdbId).toBe(155);
    });

    const req = httpMock.expectOne(request => request.url.endsWith('/api/v1/titles/155'));
    expect(req.request.method).toBe('GET');
    req.flush(mockDetail);
  });

  it('should fetch title seasons by id', () => {
    const mockSeasons = [
      { id: 1, tmdbId: 100, seasonNumber: 1, name: 'Season 1', episodeCount: 8, episodes: [] }
    ] as any;

    service.getTitleSeasons(50).subscribe(res => {
      expect(res.length).toBe(1);
      expect(res[0].name).toBe('Season 1');
    });

    const req = httpMock.expectOne(request => request.url.endsWith('/api/v1/titles/50/seasons'));
    expect(req.request.method).toBe('GET');
    req.flush(mockSeasons);
  });

  it('should fetch where-to-watch providers by id', () => {
    const mockProviders = [
      { providerId: 1, tmdbId: 8, name: 'Netflix', logoPath: '/n.jpg', type: 'flatrate', country: 'US' }
    ] as any;

    service.getTitleWhereToWatch(50).subscribe(res => {
      expect(res.length).toBe(1);
      expect(res[0].name).toBe('Netflix');
    });

    const req = httpMock.expectOne(request => request.url.endsWith('/api/v1/titles/50/where-to-watch'));
    expect(req.request.method).toBe('GET');
    req.flush(mockProviders);
  });
});
