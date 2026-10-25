import { Component, input, viewChild, ElementRef, ChangeDetectionStrategy } from '@angular/core';
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
export class ContentRowComponent {
  readonly row = input.required<ContentRow>();
  readonly scrollContainer = viewChild<ElementRef<HTMLDivElement>>('scrollContainer');

  scrollLeft(): void {
    const el = this.scrollContainer()?.nativeElement;
    if (el) {
      const scrollAmount = el.clientWidth * 0.75;
      el.scrollBy({ left: -scrollAmount, behavior: 'smooth' });
    }
  }

  scrollRight(): void {
    const el = this.scrollContainer()?.nativeElement;
    if (el) {
      const scrollAmount = el.clientWidth * 0.75;
      el.scrollBy({ left: scrollAmount, behavior: 'smooth' });
    }
  }
}
