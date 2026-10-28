import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { PlayerComponent } from './player.component';
import { CatalogService } from '../../core/services/catalog.service';
import { UserDataService } from '../../core/services/user-data.service';
import { TitleDetail, TitleSource } from '../../core/models/media.models';

describe('PlayerComponent', () => {
  let component: PlayerComponent;
  let fixture: ComponentFixture<PlayerComponent>;
  let catalogServiceMock: any;
  let userDataServiceMock: any;
  let routerMock: any;
  let activatedRouteMock: any;

  const mockTitle: TitleDetail = {
    id: 1,
    tmdbId: 100,
    type: 'series',
    title: 'Stranger Things',
    originalTitle: null,
    tagline: 'Every ending has a beginning',
    overview: 'When a young boy vanishes...',
    posterPath: '/poster.jpg',
    backdropPath: '/backdrop.jpg',
    rating: 8.7,
    voteCount: 15000,
    releaseDate: '2016-07-15',
    year: '2016',
    runtimeMinutes: null,
    status: 'Returning Series',
    contentRating: 'TV-14',
    originalLanguage: 'en',
    totalSeasons: 2,
    totalEpisodes: 16,
    genres: ['Drama', 'Sci-Fi'],
    cast: [],
    videos: [],
    providers: [],
    seasons: [
      {
        id: 1,
        tmdbId: 101,
        seasonNumber: 1,
        name: 'Season 1',
        overview: null,
        posterPath: null,
        airDate: '2016-07-15',
        episodeCount: 2,
        episodes: [
          { id: 1, tmdbId: 1001, episodeNumber: 1, name: 'Chapter One: The Vanishing', overview: null, stillPath: null, airDate: null, runtimeMinutes: 48 },
          { id: 2, tmdbId: 1002, episodeNumber: 2, name: 'Chapter Two: The Weirdo', overview: null, stillPath: null, airDate: null, runtimeMinutes: 55 }
        ]
      },
      {
        id: 2,
        tmdbId: 102,
        seasonNumber: 2,
        name: 'Season 2',
        overview: null,
        posterPath: null,
        airDate: '2017-10-27',
        episodeCount: 2,
        episodes: [
          { id: 3, tmdbId: 1003, episodeNumber: 1, name: 'Chapter One: MADMAX', overview: null, stillPath: null, airDate: null, runtimeMinutes: 48 },
          { id: 4, tmdbId: 1004, episodeNumber: 2, name: 'Chapter Two: Trick or Treat', overview: null, stillPath: null, airDate: null, runtimeMinutes: 56 }
        ]
      }
    ]
  };

  const mockSources: TitleSource[] = [
    {
      id: 1,
      providerName: 'VidSrc',
      sourceType: 'embed',
      url: 'https://vidsrc.to/embed/tv/100/1/1',
      quality: '1080p',
      label: 'Server 1 (VidSrc)',
      priority: 1,
      isDirectStream: false
    },
    {
      id: 2,
      providerName: 'VidSrc Mirror',
      sourceType: 'embed',
      url: 'https://vidsrc.me/embed/tv/100/1/1',
      quality: 'Auto',
      label: 'Server 2 (Mirror)',
      priority: 2,
      isDirectStream: false
    }
  ];

  beforeEach(async () => {
    catalogServiceMock = {
      getTitleDetail: vi.fn().mockReturnValue(of(mockTitle)),
      getTitleSources: vi.fn().mockReturnValue(of(mockSources)),
      reportSource: vi.fn().mockReturnValue(of({ success: true, message: 'Report received' }))
    };

    userDataServiceMock = {
      updateProgress: vi.fn().mockReturnValue(of({ success: true }))
    };

    routerMock = {
      navigate: vi.fn()
    };

    activatedRouteMock = {
      paramMap: of(new Map([['type', 'series'], ['id', '100']])),
      queryParamMap: of(new Map([['season', '1'], ['ep', '1']]))
    };

    await TestBed.configureTestingModule({
      imports: [PlayerComponent],
      providers: [
        { provide: CatalogService, useValue: catalogServiceMock },
        { provide: UserDataService, useValue: userDataServiceMock },
        { provide: Router, useValue: routerMock },
        { provide: ActivatedRoute, useValue: activatedRouteMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PlayerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture?.destroy();
    TestBed.resetTestingModule();
    vi.clearAllMocks();
  });

  it('should create and load title with sources', () => {
    expect(component).toBeTruthy();
    expect(component.titleId()).toBe(100);
    expect(component.mediaType()).toBe('series');
    expect(component.sources().length).toBe(2);
    expect(component.selectedSource()?.label).toBe('Server 1 (VidSrc)');
  });

  it('should toggle lights off cinema mode', () => {
    expect(component.isLightsOff()).toBe(false);
    component.toggleLights();
    expect(component.isLightsOff()).toBe(true);
    component.toggleLights();
    expect(component.isLightsOff()).toBe(false);
  });

  it('should toggle theater mode', () => {
    expect(component.isTheaterMode()).toBe(false);
    component.toggleTheater();
    expect(component.isTheaterMode()).toBe(true);
  });

  it('should toggle ad shield on and off', () => {
    expect(component.isAdShieldActive()).toBe(true);
    component.toggleAdShield();
    expect(component.isAdShieldActive()).toBe(false);
    component.toggleAdShield();
    expect(component.isAdShieldActive()).toBe(true);
  });

  it('should switch selected source', () => {
    component.selectSource(mockSources[1]);
    expect(component.selectedSource()?.id).toBe(2);
    expect(component.selectedSource()?.label).toBe('Server 2 (Mirror)');
  });

  it('should navigate to next episode', () => {
    component.goToNextEpisode();
    expect(routerMock.navigate).toHaveBeenCalledWith([], {
      relativeTo: activatedRouteMock,
      queryParams: { season: 1, ep: 2 },
      queryParamsHandling: 'merge'
    });
  });

  it('should submit report for active source', () => {
    component.openReportModal();
    expect(component.showReportModal()).toBe(true);

    component.reportReason.set('audio');
    component.reportDetails.set('Audio desynced');
    component.submitReport();

    expect(catalogServiceMock.reportSource).toHaveBeenCalledWith(1, {
      titleId: 100,
      reason: 'audio',
      details: 'Audio desynced',
      season: 1,
      episode: 1
    });
    expect(component.reportSuccessMessage()).toBe('Report received');
  });
});
