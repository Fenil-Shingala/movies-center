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

  readonly isInstallBannerDismissed = signal<boolean>(false);

  dismissInstallBanner(): void {
    this.isInstallBannerDismissed.set(true);
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
