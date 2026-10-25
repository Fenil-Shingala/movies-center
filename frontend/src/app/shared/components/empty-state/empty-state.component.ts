import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideFilm, LucideRotateCcw } from '@lucide/angular';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule, LucideFilm, LucideRotateCcw],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmptyStateComponent {
  readonly title = input<string>('No titles found');
  readonly message = input<string>('There are no titles matching your selected filters. Try broadening your criteria.');
  readonly showReset = input<boolean>(true);

  readonly reset = output<void>();

  onReset(): void {
    this.reset.emit();
  }
}
