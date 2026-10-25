import { Component, input, output, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideChevronLeft, LucideChevronRight } from '@lucide/angular';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule, LucideChevronLeft, LucideChevronRight],
  templateUrl: './pagination.component.html',
  styleUrl: './pagination.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PaginationComponent {
  readonly page = input<number>(1);
  readonly totalPages = input<number>(1);
  readonly hasPreviousPage = input<boolean>(false);
  readonly hasNextPage = input<boolean>(false);

  readonly pageChange = output<number>();

  readonly pages = computed(() => {
    const current = this.page();
    const total = this.totalPages();
    const visible: number[] = [];

    const start = Math.max(1, current - 2);
    const end = Math.min(total, current + 2);

    for (let i = start; i <= end; i++) {
      visible.push(i);
    }
    return visible;
  });

  onSelectPage(p: number): void {
    if (p !== this.page() && p >= 1 && p <= this.totalPages()) {
      this.pageChange.emit(p);
    }
  }

  onPrev(): void {
    if (this.hasPreviousPage()) {
      this.pageChange.emit(this.page() - 1);
    }
  }

  onNext(): void {
    if (this.hasNextPage()) {
      this.pageChange.emit(this.page() + 1);
    }
  }
}
