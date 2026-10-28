import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { signal } from '@angular/core';
import { NotificationSettingsModalComponent } from './notification-settings-modal.component';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationSettings } from '../../../core/models/notification.models';
import { PwaService } from '../../../core/services/pwa.service';

describe('NotificationSettingsModalComponent', () => {
  let component: NotificationSettingsModalComponent;
  let fixture: ComponentFixture<NotificationSettingsModalComponent>;

  const initialSettings: NotificationSettings = {
    newEpisodeInApp: true,
    newEpisodeEmail: true,
    newEpisodePush: false,
    continueWatchingInApp: true,
    continueWatchingEmail: false,
    continueWatchingPush: false
  };

  const mockNotificationService = {
    settings: signal<NotificationSettings | null>(initialSettings),
    isSavingSettings: signal<boolean>(false),
    loadSettings: vi.fn().mockReturnValue(of(initialSettings)),
    updateSettings: vi.fn().mockReturnValue(of(initialSettings)),
    closeSettingsModal: vi.fn()
  };

  const mockPwaService = {
    subscribeToPush: vi.fn().mockResolvedValue(true),
    unsubscribeFromPush: vi.fn().mockResolvedValue(true),
    isPushSubscribed: signal<boolean>(false)
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotificationSettingsModalComponent],
      providers: [
        { provide: NotificationService, useValue: mockNotificationService },
        { provide: PwaService, useValue: mockPwaService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationSettingsModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture?.destroy();
    TestBed.resetTestingModule();
    vi.clearAllMocks();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should toggle setting keys properly', () => {
    const initial = component.settingsModel().newEpisodeEmail;
    component.toggle('newEpisodeEmail');
    expect(component.settingsModel().newEpisodeEmail).toBe(!initial);
  });

  it('should call pwaService.subscribeToPush when toggling newEpisodePush to true', async () => {
    component.settingsModel.set({ ...initialSettings, newEpisodePush: false });
    await component.toggle('newEpisodePush');
    expect(mockPwaService.subscribeToPush).toHaveBeenCalled();
    expect(component.settingsModel().newEpisodePush).toBe(true);
  });

  it('should call pwaService.unsubscribeFromPush when toggling newEpisodePush to false', async () => {
    component.settingsModel.set({ ...initialSettings, newEpisodePush: true });
    await component.toggle('newEpisodePush');
    expect(mockPwaService.unsubscribeFromPush).toHaveBeenCalled();
    expect(component.settingsModel().newEpisodePush).toBe(false);
  });

  it('should call updateSettings on save', () => {
    component.save();
    expect(mockNotificationService.updateSettings).toHaveBeenCalledWith(component.settingsModel());
  });

  it('should call closeSettingsModal on close', () => {
    component.close();
    expect(mockNotificationService.closeSettingsModal).toHaveBeenCalled();
  });
});
