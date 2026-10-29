import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ReviewsService } from './reviews.service';
import { ReviewsResponse, ReviewItem } from '../models/media.models';

describe('ReviewsService', () => {
  let service: ReviewsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ReviewsService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(ReviewsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch reviews for a title', () => {
    const mockResponse: ReviewsResponse = {
      items: [
        {
          id: 1,
          titleId: 101,
          userId: 'user-1',
          userName: 'Alice',
          userAvatar: null,
          content: 'Great movie!',
          ratingScore: 5,
          containsSpoilers: false,
          likesCount: 3,
          isLikedByMe: false,
          isMyReview: false,
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString()
        }
      ],
      totalCount: 1,
      page: 1,
      pageSize: 10,
      totalPages: 1
    };

    service.getReviews(101, 1, 10, 'popular').subscribe(res => {
      expect(res.items).toHaveLength(1);
      expect(res.items[0].content).toBe('Great movie!');
    });

    const req = httpMock.expectOne(r => r.url === '/api/v1/titles/101/reviews' && r.params.get('sort') === 'popular');
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should create a review', () => {
    const newReview: ReviewItem = {
      id: 2,
      titleId: 101,
      userId: 'user-2',
      userName: 'Bob',
      userAvatar: null,
      content: 'Exciting film!',
      ratingScore: 4,
      containsSpoilers: false,
      likesCount: 0,
      isLikedByMe: false,
      isMyReview: true,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    };

    service.createReview(101, { content: 'Exciting film!', ratingScore: 4 }).subscribe(res => {
      expect(res.id).toBe(2);
      expect(res.isMyReview).toBe(true);
    });

    const req = httpMock.expectOne('/api/v1/titles/101/reviews');
    expect(req.request.method).toBe('POST');
    req.flush(newReview);
  });

  it('should toggle like on a review', () => {
    service.toggleLike(2).subscribe(res => {
      expect(res.isLiked).toBe(true);
      expect(res.likesCount).toBe(1);
    });

    const req = httpMock.expectOne('/api/v1/reviews/2/like');
    expect(req.request.method).toBe('POST');
    req.flush({ isLiked: true, likesCount: 1 });
  });

  it('should delete a review', () => {
    service.deleteReview(2).subscribe(res => {
      expect(res.success).toBe(true);
    });

    const req = httpMock.expectOne('/api/v1/reviews/2');
    expect(req.request.method).toBe('DELETE');
    req.flush({ success: true, message: 'Deleted' });
  });
});
