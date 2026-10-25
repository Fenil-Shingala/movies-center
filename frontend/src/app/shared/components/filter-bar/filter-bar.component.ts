import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideRotateCcw, LucideArrowDownUp, LucideCalendar, LucideLayers } from '@lucide/angular';
import { GenreItem } from '../../../core/models/media.models';

export interface SortOption {
  label: string;
  value: string;
}

@Component({
  selector: 'app-filter-bar',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideRotateCcw,
    LucideArrowDownUp,
    LucideCalendar,
    LucideLayers
  ],
  templateUrl: './filter-bar.component.html',
  styleUrl: './filter-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FilterBarComponent {
  readonly genres = input<GenreItem[]>([]);
  readonly selectedGenre = input<number | null>(null);
  readonly selectedYear = input<number | null>(null);
  readonly selectedSort = input<string>('popularity.desc');
  readonly totalResults = input<number>(0);

  readonly genreChange = output<number | null>();
  readonly yearChange = output<number | null>();
  readonly sortChange = output<string>();
  readonly resetFilters = output<void>();

  readonly sortOptions: SortOption[] = [
    { label: 'Most Popular', value: 'popularity.desc' },
    { label: 'Highest Rated', value: 'rating.desc' },
    { label: 'Newest Release', value: 'release_date.desc' },
    { label: 'Title (A-Z)', value: 'title.asc' }
  ];

  readonly yearOptions: number[] = [
    2026, 2025, 2024, 2023, 2022, 2021, 2020, 2019, 2018, 2015, 2010
  ];

  onGenreSelect(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const parsed = value ? parseInt(value, 10) : null;
    this.genreChange.emit(parsed);
  }

  onYearSelect(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const parsed = value ? parseInt(value, 10) : null;
    this.yearChange.emit(parsed);
  }

  onSortSelect(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.sortChange.emit(value);
  }

  onReset(): void {
    this.resetFilters.emit();
  }

  get hasActiveFilters(): boolean {
    return this.selectedGenre() !== null || this.selectedYear() !== null || this.selectedSort() !== 'popularity.desc';
  }
}
