import { describe, it, expect, beforeEach } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HeroBannerComponent } from './hero-banner.component';
import { HeroItem } from '../../../../core/models/media.models';

describe('HeroBannerComponent', () => {
  let component: HeroBannerComponent;
  let fixture: ComponentFixture<HeroBannerComponent>;

  const mockHero: HeroItem = {
    tmdbId: 202,
    type: 'movie',
    title: 'Interstellar',
    overview: 'Mankind was born on Earth. It was never meant to die here.',
    backdropPath: '/interstellar_b.jpg',
    posterPath: '/interstellar_p.jpg',
    rating: 8.7,
    releaseDate: '2014-11-05',
    year: '2014',
    runtimeMinutes: 169,
    totalSeasons: null,
    genres: ['Adventure', 'Drama', 'Science Fiction'],
    quality: '4K Ultra HD'
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HeroBannerComponent],
      providers: [provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(HeroBannerComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('hero', mockHero);
    fixture.detectChanges();
  });

  it('should create and render hero details', () => {
    expect(component).toBeTruthy();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.hero-title')?.textContent).toContain('Interstellar');
    expect(compiled.querySelector('.rating')?.textContent).toContain('8.7');
    expect(compiled.querySelector('.runtime')?.textContent).toContain('2h 49m');
  });

  it('should compute valid backdrop URL', () => {
    expect(component.backdropUrl()).toBe('https://image.tmdb.org/t/p/w1280/interstellar_b.jpg');
  });
});
