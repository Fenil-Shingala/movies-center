import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  LucideDownload,
  LucideRefreshCw,
  LucideWifiOff,
  LucideX,
  LucideFilm
} from '@lucide/angular';
import { PwaService } from '../../../core/services/pwa.service';

@Component({
  selector: 'app-pwa-prompt',
  standalone: true,
  imports: [
    CommonModule,
    LucideDownload,
    LucideRefreshCw,
    LucideWifiOff,
    LucideX,
    LucideFilm
  ],
  templateUrl: './pwa-prompt.component.html',
  styleUrls: ['./pwa-prompt.component.scss']
})
export class PwaPromptComponent {
  readonly pwaService = inject(PwaService);

  readonly isInstallBannerDismissed = signal<boolean>(
    typeof window !== 'undefined' && sessionStorage.getItem('pwa_install_dismissed') === 'true'
  );

  dismissInstallBanner(): void {
    this.isInstallBannerDismissed.set(true);
    if (typeof window !== 'undefined') {
      try {
        sessionStorage.setItem('pwa_install_dismissed', 'true');
      } catch {
        // Safe fallback for restricted storage environments
      }
    }
  }

  installApp(): void {
    this.pwaService.promptInstall();
  }

  updateApp(): void {
    this.pwaService.applyUpdate();
  }

  dismissUpdate(): void {
    this.pwaService.dismissUpdate();
  }
}
