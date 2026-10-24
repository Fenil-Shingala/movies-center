import { Component, OnInit, signal, inject, PLATFORM_ID, ChangeDetectionStrategy } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { COOKIE_CONSENT_KEY } from '../../core/constants/app.constants';

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
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem(COOKIE_CONSENT_KEY, 'accepted');
    }
    this.isVisible.set(false);
  }

  reject(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem(COOKIE_CONSENT_KEY, 'rejected');
    }
    this.isVisible.set(false);
  }
}
