import { Component, OnInit, signal, inject, PLATFORM_ID, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { HomeService } from '../../core/services/home.service';
import { HomeFeed } from '../../core/models/media.models';
import { HeroBannerComponent } from './components/hero-banner/hero-banner.component';
import { ContentRowComponent } from './components/content-row/content-row.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, HeroBannerComponent, ContentRowComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent implements OnInit {
  private readonly homeService = inject(HomeService);
  private readonly platformId = inject(PLATFORM_ID);

  readonly feed = signal<HomeFeed | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.loadHomeFeed();
    }
  }

  loadHomeFeed(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.homeService.getHomeFeed().subscribe({
      next: (data) => {
        this.feed.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load home feed', err);
        this.errorMessage.set('Unable to load catalog content. Please check your connection or try again.');
        this.isLoading.set(false);
      }
    });
  }
}
