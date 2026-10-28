import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { PwaPromptComponent } from './pwa-prompt.component';
import { PwaService } from '../../../core/services/pwa.service';

describe('PwaPromptComponent', () => {
  let component: PwaPromptComponent;
  let fixture: ComponentFixture<PwaPromptComponent>;

  const mockPwaService = {
    canInstall: signal<boolean>(false),
    isInstalled: signal<boolean>(false),
    hasUpdate: signal<boolean>(false),
    isOffline: signal<boolean>(false),
    promptInstall: vi.fn().mockResolvedValue(true),
    applyUpdate: vi.fn(),
    dismissUpdate: vi.fn()
  };

  beforeEach(async () => {
    mockPwaService.canInstall.set(false);
    mockPwaService.isInstalled.set(false);
    mockPwaService.hasUpdate.set(false);
    mockPwaService.isOffline.set(false);

    await TestBed.configureTestingModule({
      imports: [PwaPromptComponent],
      providers: [
        { provide: PwaService, useValue: mockPwaService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PwaPromptComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
    vi.clearAllMocks();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should trigger pwaService.promptInstall on installApp()', () => {
    component.installApp();
    expect(mockPwaService.promptInstall).toHaveBeenCalled();
  });

  it('should trigger pwaService.applyUpdate on updateApp()', () => {
    component.updateApp();
    expect(mockPwaService.applyUpdate).toHaveBeenCalled();
  });

  it('should trigger pwaService.dismissUpdate on dismissUpdate()', () => {
    component.dismissUpdate();
    expect(mockPwaService.dismissUpdate).toHaveBeenCalled();
  });

  it('should dismiss install banner when dismissInstallBanner() is called', () => {
    expect(component.isInstallBannerDismissed()).toBe(false);
    component.dismissInstallBanner();
    expect(component.isInstallBannerDismissed()).toBe(true);
  });

  it('should render offline banner when isOffline is true', () => {
    mockPwaService.isOffline.set(true);
    fixture.detectChanges();
    const banner = fixture.nativeElement.querySelector('.pwa-offline-bar');
    expect(banner).toBeTruthy();
    expect(banner.textContent).toContain('offline');
  });

  it('should render update banner when hasUpdate is true', () => {
    mockPwaService.hasUpdate.set(true);
    fixture.detectChanges();
    const banner = fixture.nativeElement.querySelector('.pwa-update-toast');
    expect(banner).toBeTruthy();
    expect(banner.textContent).toContain('New Version Available');
  });

  it('should render install banner when canInstall is true and not dismissed', () => {
    mockPwaService.canInstall.set(true);
    fixture.detectChanges();
    const banner = fixture.nativeElement.querySelector('.pwa-install-banner');
    expect(banner).toBeTruthy();
  });
});
