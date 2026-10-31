import { 
  Component, 
  input, 
  viewChild, 
  ElementRef, 
  ChangeDetectionStrategy, 
  signal, 
  computed,
  AfterViewInit, 
  OnDestroy 
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LucideChevronLeft, LucideChevronRight } from '@lucide/angular';
import { ContentRow } from '../../../../core/models/media.models';
import { MediaCardComponent } from '../media-card/media-card.component';

@Component({
  selector: 'app-content-row',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideChevronLeft, LucideChevronRight, MediaCardComponent],
  templateUrl: './content-row.component.html',
  styleUrl: './content-row.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ContentRowComponent implements AfterViewInit, OnDestroy {
  readonly row = input.required<ContentRow>();
  readonly scrollContainer = viewChild<ElementRef<HTMLDivElement>>('scrollContainer');

  readonly canScrollLeft = signal<boolean>(false);
  readonly canScrollRight = signal<boolean>(true);

  readonly viewAllPath = computed(() => {
    const rawRoute = this.row()?.viewAllRoute?.trim() || (this.row()?.type === 'series' ? '/series' : '/movies');
    return rawRoute.split('?')[0] || (this.row()?.type === 'series' ? '/series' : '/movies');
  });

  readonly viewAllQueryParams = computed<Record<string, string>>(() => {
    const rawRoute = this.row()?.viewAllRoute?.trim() || '';
    if (!rawRoute.includes('?')) return {};
    const queryStr = rawRoute.split('?')[1] || '';
    const params = new URLSearchParams(queryStr);
    const result: Record<string, string> = {};
    params.forEach((value, key) => {
      let mapped = value;
      if (key === 'sort') {
        if (mapped === 'popular' || mapped === 'trending') mapped = 'popularity.desc';
        else if (mapped === 'top_rated' || mapped === 'rating') mapped = 'rating.desc';
        else if (mapped === 'upcoming' || mapped === 'newest') mapped = 'release_date.desc';
      }
      result[key] = mapped;
    });
    return result;
  });

  private resizeObserver?: ResizeObserver;

  ngAfterViewInit(): void {
    this.updateScrollState();

    const el = this.scrollContainer()?.nativeElement;
    if (el && typeof ResizeObserver !== 'undefined') {
      this.resizeObserver = new ResizeObserver(() => {
        this.updateScrollState();
      });
      this.resizeObserver.observe(el);
    }
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }

  updateScrollState(): void {
    const el = this.scrollContainer()?.nativeElement;
    if (!el) return;

    const left = el.scrollLeft ?? 0;
    const clientWidth = el.clientWidth ?? 0;
    const scrollWidth = el.scrollWidth ?? clientWidth;
    const maxScroll = scrollWidth - clientWidth;

    this.canScrollLeft.set(left > 10);
    this.canScrollRight.set(maxScroll > 10 && left < maxScroll - 10);
  }

  scrollLeft(): void {
    const el = this.scrollContainer()?.nativeElement;
    if (el) {
      const scrollAmount = (el.clientWidth || 800) * 0.75;
      el.scrollBy({ left: -scrollAmount, behavior: 'smooth' });
      setTimeout(() => this.updateScrollState(), 350);
    }
  }

  scrollRight(): void {
    const el = this.scrollContainer()?.nativeElement;
    if (el) {
      const scrollAmount = (el.clientWidth || 800) * 0.75;
      el.scrollBy({ left: scrollAmount, behavior: 'smooth' });
      setTimeout(() => this.updateScrollState(), 350);
    }
  }
}
