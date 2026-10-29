import { Component, OnInit, signal, inject, PLATFORM_ID, ChangeDetectionStrategy } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { COOKIE_CONSENT_KEY } from '../../core/constants/app.constants';
import { AnalyticsService } from '../../core/services/analytics.service';

@Component({
  selector: 'app-cookie-notice',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cookie-notice.component.html',
  styleUrl: './cookie-notice.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CookieNoticeComponent implements OnInit {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly analyticsService = inject(AnalyticsService);
  readonly isVisible = signal<boolean>(false);

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      const consent = localStorage.getItem(COOKIE_CONSENT_KEY);
      if (!consent) {
        // Show banner after brief delay for smooth appearance
        setTimeout(() => this.isVisible.set(true), 500);
      }
    }
  }

  accept(): void {
    this.analyticsService.grantConsent();
    this.isVisible.set(false);
  }

  reject(): void {
    this.analyticsService.revokeConsent();
    this.isVisible.set(false);
  }
}
