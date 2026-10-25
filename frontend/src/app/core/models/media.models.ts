export interface MediaCard {
  tmdbId: number;
  type: 'movie' | 'series';
  title: string;
  posterPath: string | null;
  backdropPath: string | null;
  rating: number;
  year: string;
  genres: string[];
}

export interface HeroItem {
  tmdbId: number;
  type: 'movie' | 'series';
  title: string;
  overview: string;
  backdropPath: string | null;
  posterPath: string | null;
  rating: number;
  releaseDate: string;
  year: string;
  runtimeMinutes: number | null;
  totalSeasons: number | null;
  genres: string[];
  quality: string;
}

export interface ContentRow {
  id: string;
  title: string;
  type: 'movie' | 'series';
  viewAllRoute: string;
  items: MediaCard[];
}

export interface HomeFeed {
  hero: HeroItem;
  rows: ContentRow[];
}
