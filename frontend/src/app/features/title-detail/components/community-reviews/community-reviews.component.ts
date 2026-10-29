import { 
  Component, 
  OnInit, 
  input, 
  output, 
  signal, 
  inject, 
  ChangeDetectionStrategy,
  effect
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { 
  LucideMessageSquare, 
  LucideSend, 
  LucideStar, 
  LucideTrash2, 
  LucideEye, 
  LucideEyeOff, 
  LucideThumbsUp 
} from '@lucide/angular';
import { AuthService } from '../../../../core/services/auth.service';
import { ReviewsService } from '../../../../core/services/reviews.service';
import { ReviewItem } from '../../../../core/models/media.models';

@Component({
  selector: 'app-community-reviews',
  standalone: true,
  imports: [
    CommonModule,
    LucideMessageSquare,
    LucideSend,
    LucideStar,
    LucideTrash2,
    LucideEye,
    LucideEyeOff,
    LucideThumbsUp
  ],
  templateUrl: './community-reviews.component.html',
  styleUrl: './community-reviews.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CommunityReviewsComponent implements OnInit {
  readonly titleId = input.required<number>();
  readonly titleName = input.required<string>();
  readonly reviewSubmitted = output<void>();

  readonly authService = inject(AuthService);
  private readonly reviewsService = inject(ReviewsService);

  readonly reviews = signal<ReviewItem[]>([]);
  readonly totalReviews = signal<number>(0);
  readonly reviewsPage = signal<number>(1);
  readonly reviewsTotalPages = signal<number>(1);
  readonly reviewsSort = signal<'popular' | 'newest'>('popular');
  readonly isLoadingReviews = signal<boolean>(false);
  readonly isSubmittingReview = signal<boolean>(false);
  readonly isWritingReview = signal<boolean>(false);
  readonly newReviewContent = signal<string>('');
  readonly newReviewRating = signal<number | null>(null);
  readonly newReviewSpoilers = signal<boolean>(false);
  readonly reviewFormError = signal<string | null>(null);
  readonly revealedSpoilers = signal<Set<number>>(new Set());

  constructor() {
    effect(() => {
      // Reload reviews when user logs in or out
      const user = this.authService.currentUser();
      const id = this.titleId();
      if (id) {
        this.loadReviews(id, 1, false);
      }
    });
  }

  ngOnInit(): void {
    this.loadReviews(this.titleId(), 1, false);
  }

  loadReviews(titleId: number, page: number = 1, append: boolean = false): void {
    this.isLoadingReviews.set(true);
    this.reviewsService.getReviews(titleId, page, 10, this.reviewsSort()).subscribe({
      next: (res) => {
        if (append) {
          this.reviews.update(prev => [...prev, ...res.items]);
        } else {
          this.reviews.set(res.items);
        }
        this.totalReviews.set(res.totalCount);
        this.reviewsPage.set(res.page);
        this.reviewsTotalPages.set(res.totalPages);
        this.isLoadingReviews.set(false);
      },
      error: () => {
        if (!append) this.reviews.set([]);
        this.isLoadingReviews.set(false);
      }
    });
  }

  setReviewsSort(sort: 'popular' | 'newest'): void {
    if (this.reviewsSort() === sort) return;
    this.reviewsSort.set(sort);
    this.loadReviews(this.titleId(), 1, false);
  }

  toggleWriteReview(): void {
    if (!this.authService.isAuthenticated()) {
      this.authService.openAuthModal('login');
      return;
    }
    this.isWritingReview.update(v => !v);
    this.reviewFormError.set(null);
  }

  selectReviewRating(score: number): void {
    if (this.newReviewRating() === score) {
      this.newReviewRating.set(null);
    } else {
      this.newReviewRating.set(score);
    }
  }

  toggleSpoiler(reviewId: number): void {
    this.revealedSpoilers.update(set => {
      const next = new Set(set);
      if (next.has(reviewId)) {
        next.delete(reviewId);
      } else {
        next.add(reviewId);
      }
      return next;
    });
  }

  submitReview(): void {
    if (!this.authService.isAuthenticated()) {
      this.authService.openAuthModal('login');
      return;
    }
    const content = this.newReviewContent().trim();
    if (content.length < 3) {
      this.reviewFormError.set('Please write at least 3 characters.');
      return;
    }

    this.isSubmittingReview.set(true);
    this.reviewFormError.set(null);
    this.reviewsService.createReview(this.titleId(), {
      content,
      ratingScore: this.newReviewRating(),
      containsSpoilers: this.newReviewSpoilers()
    }).subscribe({
      next: (created) => {
        this.reviews.update(list => [created, ...list]);
        this.totalReviews.update(c => c + 1);
        this.newReviewContent.set('');
        this.newReviewRating.set(null);
        this.newReviewSpoilers.set(false);
        this.isWritingReview.set(false);
        this.isSubmittingReview.set(false);
        this.reviewSubmitted.emit();
      },
      error: (err) => {
        this.reviewFormError.set(err?.error?.message || 'Failed to submit review.');
        this.isSubmittingReview.set(false);
      }
    });
  }

  deleteReview(reviewId: number): void {
    this.reviewsService.deleteReview(reviewId).subscribe({
      next: () => {
        this.reviews.update(list => list.filter(r => r.id !== reviewId));
        this.totalReviews.update(c => Math.max(0, c - 1));
      }
    });
  }

  toggleLikeReview(review: ReviewItem): void {
    if (!this.authService.isAuthenticated()) {
      this.authService.openAuthModal('login');
      return;
    }
    this.reviewsService.toggleLike(review.id).subscribe({
      next: (res) => {
        this.reviews.update(list => list.map(r => {
          if (r.id === review.id) {
            return {
              ...r,
              isLikedByMe: res.isLiked,
              likesCount: res.likesCount
            };
          }
          return r;
        }));
      }
    });
  }

  loadMoreReviews(): void {
    const next = this.reviewsPage() + 1;
    if (next <= this.reviewsTotalPages()) {
      this.loadReviews(this.titleId(), next, true);
    }
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .slice(0, 2)
      .map(part => part.charAt(0).toUpperCase())
      .join('');
  }

  formatTimeAgo(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);
    if (seconds < 60) return 'Just now';
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    if (days < 30) return `${days}d ago`;
    const months = Math.floor(days / 30);
    if (months < 12) return `${months}mo ago`;
    return `${Math.floor(months / 12)}y ago`;
  }
}
