import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { 
  ReviewItem, 
  ReviewsResponse, 
  CreateReviewRequest, 
  ToggleLikeResponse 
} from '../models/media.models';

@Injectable({
  providedIn: 'root'
})
export class ReviewsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1`;

  getReviews(
    titleId: number, 
    page: number = 1, 
    pageSize: number = 10, 
    sort: 'popular' | 'newest' = 'popular'
  ): Observable<ReviewsResponse> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString())
      .set('sort', sort);

    return this.http.get<ReviewsResponse>(`${this.apiUrl}/titles/${titleId}/reviews`, { params });
  }

  createReview(titleId: number, request: CreateReviewRequest): Observable<ReviewItem> {
    return this.http.post<ReviewItem>(`${this.apiUrl}/titles/${titleId}/reviews`, request);
  }

  deleteReview(reviewId: number): Observable<{ success: boolean; message: string }> {
    return this.http.delete<{ success: boolean; message: string }>(`${this.apiUrl}/reviews/${reviewId}`);
  }

  toggleLike(reviewId: number): Observable<ToggleLikeResponse> {
    return this.http.post<ToggleLikeResponse>(`${this.apiUrl}/reviews/${reviewId}/like`, {});
  }
}
