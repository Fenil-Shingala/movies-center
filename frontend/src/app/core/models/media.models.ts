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

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface GenreItem {
  id: number;
  name: string;
  slug: string;
}

export interface BrowseFilter {
  genre?: number | null;
  year?: number | null;
  language?: string | null;
  sort?: string;
  page?: number;
  pageSize?: number;
}

export interface SearchSuggestion {
  tmdbId: number;
  type: 'movie' | 'series';
  title: string;
  posterPath: string | null;
  rating: number;
  year: string;
  genres: string[];
}

export interface CastMember {
  personId: number;
  tmdbId: number;
  name: string;
  character: string;
  profilePath: string | null;
  displayOrder: number;
}

export interface VideoItem {
  id: number;
  key: string;
  name: string;
  type: string;
  site: string;
}

export interface WatchProvider {
  providerId: number;
  tmdbId: number;
  name: string;
  logoPath: string | null;
  type: string;
  country: string;
}

export interface EpisodeItem {
  id: number;
  tmdbId: number;
  episodeNumber: number;
  name: string;
  overview: string | null;
  stillPath: string | null;
  airDate: string | null;
  runtimeMinutes: number | null;
}

export interface SeasonItem {
  id: number;
  tmdbId: number;
  seasonNumber: number;
  name: string;
  overview: string | null;
  posterPath: string | null;
  airDate: string | null;
  episodeCount: number;
  episodes: EpisodeItem[];
}

export interface TitleDetail {
  id: number;
  tmdbId: number;
  type: 'movie' | 'series';
  title: string;
  originalTitle: string | null;
  tagline: string | null;
  overview: string | null;
  posterPath: string | null;
  backdropPath: string | null;
  rating: number;
  voteCount: number;
  releaseDate: string | null;
  year: string;
  runtimeMinutes: number | null;
  status: string | null;
  contentRating: string | null;
  originalLanguage: string;
  totalSeasons: number | null;
  totalEpisodes: number | null;
  genres: string[];
  cast: CastMember[];
  videos: VideoItem[];
  providers: WatchProvider[];
  seasons: SeasonItem[];
}

export interface TitleSource {
  id: number;
  providerName: string;
  sourceType: string;
  url: string;
  quality: string;
  label: string;
  priority: number;
  isDirectStream: boolean;
}

export interface ReportSourceRequest {
  titleId: number;
  reason: string;
  details?: string | null;
  season?: number | null;
  episode?: number | null;
}

export interface SourceReportResponse {
  success: boolean;
  message: string;
}

export interface WatchlistItem {
  id: number;
  titleId: number;
  title: string;
  posterPath: string | null;
  backdropPath: string | null;
  type: 'movie' | 'series';
  releaseDate: string | null;
  year: string;
  rating: number;
  addedAt: string;
}

export interface WatchHistoryItem {
  id: number;
  titleId: number;
  title: string;
  posterPath: string | null;
  backdropPath: string | null;
  type: 'movie' | 'series';
  episodeId?: number | null;
  seasonNumber?: number | null;
  episodeNumber?: number | null;
  episodeName?: string | null;
  progressSeconds: number;
  durationSeconds?: number | null;
  progressPercent: number;
  isCompleted: boolean;
  watchedAt: string;
}

export interface RatingDto {
  averageRating: number;
  totalRatings: number;
  userRating: number | null;
  distribution: { [key: number]: number };
}

export interface SubmitRatingRequest {
  score: number;
}

