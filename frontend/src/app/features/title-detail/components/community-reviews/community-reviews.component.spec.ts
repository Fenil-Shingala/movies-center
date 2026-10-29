import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { CommunityReviewsComponent } from './community-reviews.component';
import { ReviewsService } from '../../../../core/services/reviews.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ReviewItem, ReviewsResponse } from '../../../../core/models/media.models';

describe('CommunityReviewsComponent', () => {
  let component: CommunityReviewsComponent;
  let fixture: ComponentFixture<CommunityReviewsComponent>;
  let mockReviewsService: any;
  let mockAuthService: any;

  const mockReviews: ReviewItem[] = [
    {
      id: 101,
      titleId: 1,
      userId: 'user-1',
      userName: 'FilmBuff',
      userAvatar: null,
      ratingScore: 5,
      content: 'Masterpiece from start to finish! Christopher Nolan delivered perfection.',
      containsSpoilers: false,
      likesCount: 12,
      isLikedByMe: false,
      isMyReview: false,
      createdAt: new Date().toISOString(),
      updatedAt: null
    },
    {
      id: 102,
      titleId: 1,
      userId: 'user-2',
      userName: 'SpoilerAlert',
      userAvatar: null,
      ratingScore: 4,
      content: 'The ending with Harvey Dent was shocking and tragic.',
      containsSpoilers: true,
      likesCount: 5,
      isLikedByMe: true,
      isMyReview: true,
      createdAt: new Date().toISOString(),
      updatedAt: null
    }
  ];

  const mockResponse: ReviewsResponse = {
    items: mockReviews,
    totalCount: 2,
    page: 1,
    pageSize: 10,
    totalPages: 1
  };

  beforeEach(async () => {
    mockReviewsService = {
      getReviews: vi.fn().mockReturnValue(of(mockResponse)),
      createReview: vi.fn().mockReturnValue(of({
        id: 103,
        titleId: 1,
        userId: 'current-user',
        userName: 'Me',
        userAvatar: null,
        ratingScore: 5,
        content: 'New fresh review',
        containsSpoilers: false,
        likesCount: 0,
        isLikedByMe: false,
        isMyReview: true,
        createdAt: new Date().toISOString(),
        updatedAt: null
      })),
      deleteReview: vi.fn().mockReturnValue(of({ success: true, message: 'Deleted' })),
      toggleLike: vi.fn().mockReturnValue(of({ isLiked: true, likesCount: 13 }))
    };

    mockAuthService = {
      currentUser: signal({ id: 'current-user', email: 'me@test.com', userName: 'Me' }),
      isAuthenticated: signal(true),
      isAdmin: signal(false),
      openAuthModal: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [CommunityReviewsComponent],
      providers: [
        { provide: ReviewsService, useValue: mockReviewsService },
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CommunityReviewsComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('titleId', 1);
    fixture.componentRef.setInput('titleName', 'The Dark Knight');
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture?.destroy();
    TestBed.resetTestingModule();
    vi.clearAllMocks();
  });

  it('should create and load reviews on init', () => {
    expect(component).toBeTruthy();
    expect(mockReviewsService.getReviews).toHaveBeenCalledWith(1, 1, 10, 'popular');
    expect(component.reviews().length).toBe(2);
    expect(component.totalReviews()).toBe(2);
  });

  it('should switch sort to newest and reload', () => {
    component.setReviewsSort('newest');
    expect(component.reviewsSort()).toBe('newest');
    expect(mockReviewsService.getReviews).toHaveBeenCalledWith(1, 1, 10, 'newest');
  });

  it('should toggle spoiler shield', () => {
    expect(component.revealedSpoilers().has(102)).toBe(false);
    component.toggleSpoiler(102);
    expect(component.revealedSpoilers().has(102)).toBe(true);
    component.toggleSpoiler(102);
    expect(component.revealedSpoilers().has(102)).toBe(false);
  });

  it('should toggle write review card', () => {
    expect(component.isWritingReview()).toBe(false);
    component.toggleWriteReview();
    expect(component.isWritingReview()).toBe(true);
    component.toggleWriteReview();
    expect(component.isWritingReview()).toBe(false);
  });

  it('should submit review when valid and emit reviewSubmitted', () => {
    let emitted = false;
    component.reviewSubmitted.subscribe(() => {
      emitted = true;
    });

    component.toggleWriteReview();
    component.newReviewContent.set('A brilliant cinematic achievement!');
    component.selectReviewRating(5);
    component.submitReview();

    expect(mockReviewsService.createReview).toHaveBeenCalledWith(1, {
      content: 'A brilliant cinematic achievement!',
      ratingScore: 5,
      containsSpoilers: false
    });
    expect(component.reviews().length).toBe(3);
    expect(component.isWritingReview()).toBe(false);
    expect(emitted).toBe(true);
  });

  it('should reject review content shorter than 3 characters', () => {
    component.toggleWriteReview();
    component.newReviewContent.set('Hi');
    component.submitReview();

    expect(mockReviewsService.createReview).not.toHaveBeenCalled();
    expect(component.reviewFormError()).toBe('Please write at least 3 characters.');
  });

  it('should toggle like on a review', () => {
    const target = component.reviews()[0];
    component.toggleLikeReview(target);

    expect(mockReviewsService.toggleLike).toHaveBeenCalledWith(101);
    const updated = component.reviews().find(r => r.id === 101);
    expect(updated?.isLikedByMe).toBe(true);
    expect(updated?.likesCount).toBe(13);
  });

  it('should delete review', () => {
    component.deleteReview(101);
    expect(mockReviewsService.deleteReview).toHaveBeenCalledWith(101);
    expect(component.reviews().find(r => r.id === 101)).toBeUndefined();
    expect(component.totalReviews()).toBe(1);
  });
});
