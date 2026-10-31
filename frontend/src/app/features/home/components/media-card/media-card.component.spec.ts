import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { MediaCardComponent } from './media-card.component';
import { MediaCard } from '../../../../core/models/media.models';
import { DEFAULT_POSTER_IMAGE } from '../../../../core/constants/app.constants';

describe('MediaCardComponent', () => {
  let component: MediaCardComponent;
  let fixture: ComponentFixture<MediaCardComponent>;

  const mockMovieCard: MediaCard = {
    tmdbId: 101,
    title: 'Inception',
    type: 'movie',
    posterPath: '/inception-poster.jpg',
    backdropPath: '/inception-backdrop.jpg',
    rating: 8.8,
    year: '2010',
    genres: ['Action', 'Sci-Fi']
  };

  const mockCardWithoutPoster: MediaCard = {
    tmdbId: 102,
    title: 'Unknown Title',
    type: 'series',
    posterPath: null,
    backdropPath: null,
    rating: 7.5,
    year: '2024',
    genres: ['Drama']
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MediaCardComponent],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('should render TMDB poster url when posterPath is available', () => {
    fixture = TestBed.createComponent(MediaCardComponent);
    fixture.componentRef.setInput('item', mockMovieCard);
    fixture.detectChanges();

    expect(fixture.componentInstance.posterUrl()).toBe('https://image.tmdb.org/t/p/w500/inception-poster.jpg');
    expect(fixture.componentInstance.detailRoute()).toBe('/movie/101');
  });

  it('should return DEFAULT_POSTER_IMAGE when posterPath is null', () => {
    fixture = TestBed.createComponent(MediaCardComponent);
    fixture.componentRef.setInput('item', mockCardWithoutPoster);
    fixture.detectChanges();

    expect(fixture.componentInstance.posterUrl()).toBe(DEFAULT_POSTER_IMAGE);
    expect(fixture.componentInstance.detailRoute()).toBe('/series/102');
  });

  it('should switch to DEFAULT_POSTER_IMAGE on image load error', () => {
    fixture = TestBed.createComponent(MediaCardComponent);
    fixture.componentRef.setInput('item', mockMovieCard);
    fixture.detectChanges();

    expect(fixture.componentInstance.posterUrl()).toBe('https://image.tmdb.org/t/p/w500/inception-poster.jpg');

    const fakeImg = document.createElement('img');
    fakeImg.src = 'https://image.tmdb.org/t/p/w500/inception-poster.jpg';
    const fakeEvent = { target: fakeImg } as unknown as Event;

    fixture.componentInstance.onImageError(fakeEvent);
    fixture.detectChanges();

    expect(fixture.componentInstance.imageFailed()).toBe(true);
    expect(fixture.componentInstance.posterUrl()).toBe(DEFAULT_POSTER_IMAGE);
    expect(fakeImg.src).toContain(DEFAULT_POSTER_IMAGE);
  });
});
