import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { 
  LucideX, 
  LucideBell, 
  LucideMail, 
  LucideSmartphone, 
  LucideFilm, 
  LucideClock, 
  LucideCheck, 
  LucideLoader2 
} from '@lucide/angular';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationSettings } from '../../../core/models/notification.models';
import { PwaService } from '../../../core/services/pwa.service';

@Component({
  selector: 'app-notification-settings-modal',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    LucideX, 
    LucideBell, 
    LucideMail, 
    LucideSmartphone, 
    LucideFilm, 
    LucideClock, 
    LucideCheck, 
    LucideLoader2
  ],
  templateUrl: './notification-settings-modal.component.html',
  styleUrls: ['./notification-settings-modal.component.scss']
})
export class NotificationSettingsModalComponent {
  readonly notificationService = inject(NotificationService);
  readonly pwaService = inject(PwaService);

  readonly savedSuccess = signal<boolean>(false);

  // Local editable model
  settingsModel = signal<NotificationSettings>({
    newEpisodeInApp: true,
    newEpisodeEmail: true,
    newEpisodePush: false,
    continueWatchingInApp: true,
    continueWatchingEmail: false,
    continueWatchingPush: false
  });

  constructor() {
    const current = this.notificationService.settings();
    if (current) {
      this.settingsModel.set({ ...current });
    } else {
      this.notificationService.loadSettings().subscribe(s => {
        if (s) this.settingsModel.set({ ...s });
      });
    }
  }

  async toggle(key: keyof NotificationSettings): Promise<void> {
    if (key === 'newEpisodePush') {
      const willEnable = !this.settingsModel().newEpisodePush;
      if (willEnable) {
        const success = await this.pwaService.subscribeToPush();
        this.settingsModel.update(s => ({
          ...s,
          newEpisodePush: success,
          continueWatchingPush: success
        }));
      } else {
        await this.pwaService.unsubscribeFromPush();
        this.settingsModel.update(s => ({
          ...s,
          newEpisodePush: false,
          continueWatchingPush: false
        }));
      }
      this.savedSuccess.set(false);
      return;
    }

    this.settingsModel.update(s => ({
      ...s,
      [key]: !s[key]
    }));
    this.savedSuccess.set(false);
  }

  save(): void {
    this.notificationService.updateSettings(this.settingsModel()).subscribe(res => {
      if (res) {
        this.savedSuccess.set(true);
        setTimeout(() => {
          this.savedSuccess.set(false);
          this.close();
        }, 1200);
      }
    });
  }

  close(): void {
    this.notificationService.closeSettingsModal();
  }
}
