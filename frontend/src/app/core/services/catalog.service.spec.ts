import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CatalogService } from './catalog.service';
import { PagedResult, MediaCard, GenreItem } from '../models/media.models';

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
});
