import { describe, it, expect, beforeEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { SearchComponent } from './search.component';
import { CatalogService } from '../../core/services/catalog.service';
import { PagedResult, MediaCard } from '../../core/models/media.models';

describe('SearchComponent', () => {
  let component: SearchComponent;
  let fixture: ComponentFixture<SearchComponent>;
  let mockCatalogService: Partial<CatalogService>;

  const mockPagedResult: PagedResult<MediaCard> = {
    items: [
      {
        tmdbId: 155,
        type: 'movie',
        title: 'The Dark Knight',
        posterPath: '/dark.jpg',
        backdropPath: '/dark_b.jpg',
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

  beforeEach(async () => {
    mockCatalogService = {
      searchTitles: () => of(mockPagedResult)
    };

    await TestBed.configureTestingModule({
      imports: [SearchComponent],
      providers: [
        provideRouter([]),
        { provide: CatalogService, useValue: mockCatalogService },
        {
          provide: ActivatedRoute,
          useValue: {
            queryParams: of({ q: 'dark', type: 'movie' }),
            snapshot: { queryParams: { q: 'dark', type: 'movie' } }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should read query parameters and load search results', () => {
    expect(component.queryText()).toBe('dark');
    expect(component.selectedType()).toBe('movie');
    expect(component.titles().length).toBe(1);
    expect(component.titles()[0].title).toBe('The Dark Knight');
    expect(component.totalCount()).toBe(1);
  });

  it('should change type tab', () => {
    component.setType('series');
    expect(component.selectedType()).toBe('series');
  });

  it('should handle pagination change', () => {
    component.onPageChange(2);
    expect(component.currentPage()).toBe(2);
  });
});
