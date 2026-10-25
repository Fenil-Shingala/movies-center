import { 
  Component, 
  HostListener, 
  signal, 
  inject, 
  PLATFORM_ID, 
  ElementRef, 
  ChangeDetectionStrategy, 
  OnDestroy 
} from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { Subject, Subscription, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, tap, catchError } from 'rxjs/operators';
import { 
  LucideSearch, 
  LucideUser, 
  LucideFilm, 
  LucideX, 
  LucideStar, 
  LucideArrowRight, 
  LucideLoader2 
} from '@lucide/angular';
import { APP_NAME } from '../../core/constants/app.constants';
import { CatalogService } from '../../core/services/catalog.service';
import { SearchSuggestion } from '../../core/models/media.models';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule, 
    RouterLink, 
    RouterLinkActive, 
    LucideSearch, 
    LucideUser, 
    LucideFilm, 
    LucideX, 
    LucideStar, 
    LucideArrowRight, 
    LucideLoader2
  ],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderComponent implements OnDestroy {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly router = inject(Router);
  private readonly catalogService = inject(CatalogService);
  private readonly elementRef = inject(ElementRef);

  readonly appName = APP_NAME;
  readonly isScrolled = signal<boolean>(false);
  readonly searchQuery = signal<string>('');
  readonly suggestions = signal<SearchSuggestion[]>([]);
  readonly isSearching = signal<boolean>(false);
  readonly isDropdownOpen = signal<boolean>(false);
  readonly isMobileSearchActive = signal<boolean>(false);

  private readonly searchInput$ = new Subject<string>();
  private readonly sub = new Subscription();

  constructor() {
    this.sub.add(
      this.searchInput$.pipe(
        debounceTime(300),
        distinctUntilChanged(),
        tap(query => {
          if (!query || query.trim().length < 2) {
            this.suggestions.set([]);
            this.isSearching.set(false);
          } else {
            this.isSearching.set(true);
          }
        }),
        switchMap(query => {
          if (!query || query.trim().length < 2) {
            return of([]);
          }
          return this.catalogService.getSearchSuggestions(query, 6).pipe(
            catchError(() => of([]))
          );
        })
      ).subscribe(results => {
        this.suggestions.set(results);
        this.isSearching.set(false);
        if (this.searchQuery().trim().length >= 2) {
          this.isDropdownOpen.set(true);
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    if (isPlatformBrowser(this.platformId)) {
      const scrollOffset = window.pageYOffset || document.documentElement.scrollTop || document.body.scrollTop || 0;
      this.isScrolled.set(scrollOffset > 20);
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (isPlatformBrowser(this.platformId)) {
      const clickedInside = this.elementRef.nativeElement.contains(event.target);
      if (!clickedInside) {
        this.closeSearch();
      }
    }
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    this.closeSearch();
  }

  onSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value;
    this.searchQuery.set(value);
    this.searchInput$.next(value);
    if (value.trim().length >= 2) {
      this.isDropdownOpen.set(true);
    } else {
      this.isDropdownOpen.set(false);
      this.suggestions.set([]);
    }
  }

  onSearchFocus(): void {
    if (this.searchQuery().trim().length >= 2) {
      this.isDropdownOpen.set(true);
    }
  }

  clearSearch(inputElement?: HTMLInputElement): void {
    this.searchQuery.set('');
    this.suggestions.set([]);
    this.isDropdownOpen.set(false);
    if (inputElement) {
      inputElement.value = '';
      inputElement.focus();
    }
  }

  toggleMobileSearch(): void {
    this.isMobileSearchActive.update(v => !v);
    if (!this.isMobileSearchActive()) {
      this.closeSearch();
    }
  }

  closeSearch(): void {
    this.isDropdownOpen.set(false);
    this.isMobileSearchActive.set(false);
  }

  submitSearch(): void {
    const query = this.searchQuery().trim();
    if (query) {
      this.closeSearch();
      this.router.navigate(['/search'], { queryParams: { q: query } });
    }
  }

  selectSuggestion(item: SearchSuggestion): void {
    this.closeSearch();
    this.router.navigate(['/search'], { queryParams: { q: item.title } });
  }

  getPosterUrl(path: string | null): string {
    return path ? `https://image.tmdb.org/t/p/w92${path}` : '';
  }
}
