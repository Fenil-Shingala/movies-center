import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { HomeFeed } from '../models/media.models';
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
}
