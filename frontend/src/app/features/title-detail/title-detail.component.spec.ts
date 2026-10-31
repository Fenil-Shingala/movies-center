import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { TitleDetailComponent } from './title-detail.component';
import { CatalogService } from '../../core/services/catalog.service';
import { UserDataService } from '../../core/services/user-data.service';
import { AuthService } from '../../core/services/auth.service';
import { ReviewsService } from '../../core/services/reviews.service';
import { TitleDetail } from '../../core/models/media.models';

describe('TitleDetailComponent', () => {
  let component: TitleDetailComponent;
  let fixture: ComponentFixture<TitleDetailComponent>;
  let mockCatalogService: any;
  let mockUserDataService: any;
  let mockAuthService: any;
  let mockReviewsService: any;

  const mockTitleDetail: TitleDetail = {
    id: 1,
    tmdbId: 155,
    type: 'movie',
    title: 'The Dark Knight',
    originalTitle: 'The Dark Knight',
    tagline: 'Why So Serious?',
    overview: 'Batman raises the stakes in his war on crime.',
    posterPath: '/dark.jpg',
    backdropPath: '/dark_b.jpg',
    rating: 9.0,
    voteCount: 30000,
    releaseDate: '2008-07-18',
    year: '2008',
    runtimeMinutes: 152,
    status: 'Released',
    contentRating: 'PG-13',
    originalLanguage: 'en',
    totalSeasons: null,
    totalEpisodes: null,
    genres: ['Action', 'Crime', 'Drama'],
    cast: [
      {
        personId: 1,
        tmdbId: 3894,
        name: 'Christian Bale',
        character: 'Bruce Wayne / Batman',
        profilePath: '/bale.jpg',
        displayOrder: 0
      }
    ],
    videos: [
      {
        id: 1,
        key: 'EXeTwQWrcwY',
        name: 'Official Trailer',
        type: 'Trailer',
        site: 'YouTube'
      }
    ],
    providers: [
      {
        providerId: 1,
        tmdbId: 8,
        name: 'Netflix',
        logoPath: '/netflix.jpg',
        type: 'flatrate',
        country: 'US'
      }
    ],
    seasons: []
  };

  beforeEach(async () => {
    const isWatchlistSignal = signal(false);
    mockCatalogService = {
      getTitleDetail: vi.fn().mockReturnValue(of(mockTitleDetail)),
      getTitleRating: vi.fn().mockReturnValue(of({ averageRating: 4.5, totalRatings: 10, userRating: null, distribution: {} })),
      getTitleRecommendations: vi.fn().mockReturnValue(of([])),
      submitRating: vi.fn().mockReturnValue(of({ averageRating: 5.0, totalRatings: 11, userRating: 5, distribution: {} })),
      removeRating: vi.fn().mockReturnValue(of({ averageRating: 4.5, totalRatings: 10, userRating: null, distribution: {} }))
    };

    mockReviewsService = {
      getReviews: vi.fn().mockReturnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 0 })),
      createReview: vi.fn().mockReturnValue(of({ id: 1, titleId: 155, content: 'Test review', likesCount: 0 })),
      deleteReview: vi.fn().mockReturnValue(of({ success: true, message: 'Deleted' })),
      toggleLike: vi.fn().mockReturnValue(of({ isLiked: true, likesCount: 1 }))
    };

    mockUserDataService = {
      isInWatchlist: vi.fn().mockImplementation(() => isWatchlistSignal()),
      toggleWatchlist: vi.fn().mockImplementation(() => {
        isWatchlistSignal.update(v => !v);
        return of(!isWatchlistSignal());
      })
    };

    mockAuthService = {
      currentUser: signal({ id: 'user-1', email: 'user@test.com', userName: 'testuser' }),
      isAuthenticated: signal(true),
      openAuthModal: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [TitleDetailComponent],
      providers: [
        provideRouter([]),
        { provide: CatalogService, useValue: mockCatalogService },
        { provide: UserDataService, useValue: mockUserDataService },
        { provide: AuthService, useValue: mockAuthService },
        { provide: ReviewsService, useValue: mockReviewsService },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(new Map([['id', '155']]))
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TitleDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture?.destroy();
    TestBed.resetTestingModule();
    vi.clearAllMocks();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load title details from service', () => {
    expect(component.title()).toEqual(mockTitleDetail);
    expect(component.title()?.title).toBe('The Dark Knight');
    expect(component.title()?.year).toBe('2008');
  });

  it('should identify primary trailer and open/close modal', () => {
    expect(component.primaryTrailer()).toBeTruthy();
    expect(component.primaryTrailer()?.key).toBe('EXeTwQWrcwY');

    component.openTrailer(mockTitleDetail.videos[0]);
    expect(component.activeTrailer()).toEqual(mockTitleDetail.videos[0]);

    component.closeTrailer();
    expect(component.activeTrailer()).toBeNull();
  });

  it('should toggle watchlist status', () => {
    expect(component.isInWatchlist()).toBe(false);
    component.toggleWatchlist();
    expect(mockUserDataService.toggleWatchlist).toHaveBeenCalled();
  });

  it('should submit rating and update ratingData', () => {
    component.rateTitle(5);
    expect(mockCatalogService.submitRating).toHaveBeenCalledWith(1, 5);
    expect(component.averageRating()).toBe(5.0);
  });

  it('should construct valid profile and still URLs with correct slashes', () => {
    expect(component.getProfileUrl('/bale.jpg')).toBe('https://image.tmdb.org/t/p/w185/bale.jpg');
    expect(component.getProfileUrl('bale.jpg')).toBe('https://image.tmdb.org/t/p/w185/bale.jpg');
    expect(component.getProfileUrl(null)).toBeNull();

    expect(component.getStillUrl('/ep1.jpg')).toBe('https://image.tmdb.org/t/p/w300/ep1.jpg');
    expect(component.getStillUrl('ep1.jpg')).toBe('https://image.tmdb.org/t/p/w300/ep1.jpg');
    expect(component.getStillUrl(null)).toBeNull();
  });
});
