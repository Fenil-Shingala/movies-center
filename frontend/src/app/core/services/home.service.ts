import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { HomeFeed, MediaCard } from '../models/media.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class HomeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/home`;

  getHomeFeed(): Observable<HomeFeed> {
    return this.http.get<HomeFeed>(this.apiUrl).pipe(
      shareReplay(1)
    );
  }

  getTop10(type: 'all' | 'movie' | 'series' = 'all'): Observable<MediaCard[]> {
    return this.http.get<MediaCard[]>(`${this.apiUrl}/top10`, {
      params: { type }
    });
  }
}
