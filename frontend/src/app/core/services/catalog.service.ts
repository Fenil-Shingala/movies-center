import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { MediaCard, PagedResult, GenreItem, BrowseFilter } from '../models/media.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1`;
  private genresCache$?: Observable<GenreItem[]>;

  browseTitles(type: 'movie' | 'series', filter: BrowseFilter): Observable<PagedResult<MediaCard>> {
    let params = new HttpParams().set('type', type);

    if (filter.genre != null && filter.genre > 0) {
      params = params.set('genre', filter.genre.toString());
    }
    if (filter.year != null && filter.year > 0) {
      params = params.set('year', filter.year.toString());
    }
    if (filter.language) {
      params = params.set('language', filter.language);
    }
    if (filter.sort) {
      params = params.set('sort', filter.sort);
    }
    if (filter.page != null && filter.page > 0) {
      params = params.set('page', filter.page.toString());
    }
    if (filter.pageSize != null && filter.pageSize > 0) {
      params = params.set('pageSize', filter.pageSize.toString());
    }

    return this.http.get<PagedResult<MediaCard>>(`${this.apiUrl}/titles`, { params });
  }

  getGenres(): Observable<GenreItem[]> {
    if (!this.genresCache$) {
      this.genresCache$ = this.http.get<GenreItem[]>(`${this.apiUrl}/genres`).pipe(
        shareReplay(1)
      );
    }
    return this.genresCache$;
  }
}
