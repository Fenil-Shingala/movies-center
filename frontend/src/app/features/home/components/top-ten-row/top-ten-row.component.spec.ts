import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { TopTenRowComponent } from './top-ten-row.component';
import { HomeService } from '../../../../core/services/home.service';
import { UserDataService } from '../../../../core/services/user-data.service';
import { MediaCard } from '../../../../core/models/media.models';

describe('TopTenRowComponent', () => {
  let component: TopTenRowComponent;
  let fixture: ComponentFixture<TopTenRowComponent>;
  let mockHomeService: any;
  let mockUserDataService: any;

  const mockItems: MediaCard[] = [
    {
      tmdbId: 101,
      type: 'movie',
      title: 'Top 1 Title',
      posterPath: '/top1.jpg',
      backdropPath: '/b1.jpg',
      rating: 8.9,
      year: '2026',
      genres: ['Action', 'Thriller']
    },
    {
      tmdbId: 102,
      type: 'series',
      title: 'Top 2 Title',
      posterPath: '/top2.jpg',
      backdropPath: '/b2.jpg',
      rating: 8.5,
      year: '2025',
      genres: ['Drama']
    }
  ];

  beforeEach(async () => {
    mockHomeService = {
      getTop10: vi.fn().mockReturnValue(of(mockItems))
    };

    mockUserDataService = {
      isInWatchlist: vi.fn().mockReturnValue(false),
      toggleWatchlist: vi.fn().mockReturnValue(of(true))
    };

    await TestBed.configureTestingModule({
      imports: [TopTenRowComponent],
      providers: [
        provideRouter([]),
        { provide: HomeService, useValue: mockHomeService },
        { provide: UserDataService, useValue: mockUserDataService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TopTenRowComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('initialItems', mockItems);
    fixture.detectChanges();
  });

  it('should create and display initial items', () => {
    expect(component).toBeTruthy();
    expect(component.items()).toHaveLength(2);
    expect(component.items()[0].title).toBe('Top 1 Title');
  });

  it('should switch tabs and fetch top 10 for selected category', () => {
    component.setTab('movie');
    expect(component.activeTab()).toBe('movie');
    expect(mockHomeService.getTop10).toHaveBeenCalledWith('movie');
  });

  it('should toggle watchlist when action button is clicked', () => {
    const event = new MouseEvent('click');
    const stopPropagationSpy = vi.spyOn(event, 'stopPropagation');
    const preventDefaultSpy = vi.spyOn(event, 'preventDefault');

    component.toggleWatchlist(mockItems[0], event);

    expect(stopPropagationSpy).toHaveBeenCalled();
    expect(preventDefaultSpy).toHaveBeenCalled();
    expect(mockUserDataService.toggleWatchlist).toHaveBeenCalledWith(101, expect.any(Object));
  });

  it('should scroll horizontally when navigation chevrons are triggered', () => {
    const dummyEl = {
      clientWidth: 800,
      scrollBy: vi.fn()
    };
    Object.defineProperty(component, 'scrollContainer', {
      value: () => ({ nativeElement: dummyEl })
    });

    component.scrollLeft();
    expect(dummyEl.scrollBy).toHaveBeenCalledWith({ left: -600, behavior: 'smooth' });

    component.scrollRight();
    expect(dummyEl.scrollBy).toHaveBeenCalledWith({ left: 600, behavior: 'smooth' });
  });
});
