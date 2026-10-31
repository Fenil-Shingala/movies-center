import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ContentRowComponent } from './content-row.component';
import { ContentRow } from '../../../../core/models/media.models';

describe('ContentRowComponent', () => {
  let component: ContentRowComponent;
  let fixture: ComponentFixture<ContentRowComponent>;

  const mockRow: ContentRow = {
    id: 'trending-movies',
    title: 'Upcoming Movies',
    type: 'movie',
    viewAllRoute: '/movies',
    items: [
      {
        tmdbId: 101,
        type: 'movie',
        title: 'Avengers Doomsday',
        posterPath: '/avengers.jpg',
        backdropPath: '/avengers-bg.jpg',
        rating: 8.9,
        year: '2026',
        genres: ['Action', 'Sci-Fi']
      },
      {
        tmdbId: 102,
        type: 'movie',
        title: 'Dune',
        posterPath: '/dune.jpg',
        backdropPath: '/dune-bg.jpg',
        rating: 8.5,
        year: '2026',
        genres: ['Sci-Fi', 'Adventure']
      }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ContentRowComponent],
      providers: [
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ContentRowComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('row', mockRow);
    fixture.detectChanges();
  });

  it('should create and render row title', () => {
    expect(component).toBeTruthy();
    expect(component.row().title).toBe('Upcoming Movies');
    expect(component.row().items).toHaveLength(2);
  });

  it('should scroll horizontally when navigation methods are called', () => {
    const dummyEl = {
      clientWidth: 800,
      scrollLeft: 100,
      scrollWidth: 1600,
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

  it('should update canScrollLeft and canScrollRight based on scroll offset', () => {
    const dummyEl = {
      clientWidth: 800,
      scrollLeft: 0,
      scrollWidth: 1600
    };
    Object.defineProperty(component, 'scrollContainer', {
      value: () => ({ nativeElement: dummyEl })
    });

    component.updateScrollState();
    expect(component.canScrollLeft()).toBe(false);
    expect(component.canScrollRight()).toBe(true);

    dummyEl.scrollLeft = 300;
    component.updateScrollState();
    expect(component.canScrollLeft()).toBe(true);
    expect(component.canScrollRight()).toBe(true);

    dummyEl.scrollLeft = 800; // max scroll reached
    component.updateScrollState();
    expect(component.canScrollLeft()).toBe(true);
    expect(component.canScrollRight()).toBe(false);
  });

  it('should compute viewAllPath and viewAllQueryParams accurately', () => {
    // Default test row has viewAllRoute: '/movies'
    expect(component.viewAllPath()).toBe('/movies');
    expect(component.viewAllQueryParams()).toEqual({});

    // When viewAllRoute contains query params with aliases
    fixture.componentRef.setInput('row', {
      ...mockRow,
      viewAllRoute: '/movies?sort=popular'
    });
    expect(component.viewAllPath()).toBe('/movies');
    expect(component.viewAllQueryParams()).toEqual({ sort: 'popularity.desc' });

    // When viewAllRoute contains top_rated sort
    fixture.componentRef.setInput('row', {
      ...mockRow,
      viewAllRoute: '/movies?sort=top_rated'
    });
    expect(component.viewAllPath()).toBe('/movies');
    expect(component.viewAllQueryParams()).toEqual({ sort: 'rating.desc' });
  });
});
