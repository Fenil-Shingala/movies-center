import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SwUpdate } from '@angular/service-worker';
import { signal } from '@angular/core';
import { Subject } from 'rxjs';
import { PwaService } from './pwa.service';
import { AuthService } from './auth.service';

describe('PwaService', () => {
  let service: PwaService;
  let httpMock: HttpTestingController;
  let versionUpdates$: Subject<any>;
  let mockSwUpdate: any;

  beforeEach(() => {
    versionUpdates$ = new Subject<any>();
    mockSwUpdate = {
      isEnabled: true,
      versionUpdates: versionUpdates$.asObservable(),
      activateUpdate: vi.fn().mockResolvedValue(true)
    };

    TestBed.configureTestingModule({
      providers: [
        PwaService,
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: signal(true),
            currentUser: signal({ id: '123', email: 'test@user.com' })
          }
        },
        {
          provide: SwUpdate,
          useValue: mockSwUpdate
        }
      ]
    });

    service = TestBed.inject(PwaService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    TestBed.resetTestingModule();
    vi.clearAllMocks();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should reflect initial signals', () => {
    expect(service.canInstall()).toBe(false);
    expect(service.hasUpdate()).toBe(false);
    expect(typeof service.isOffline()).toBe('boolean');
  });

  it('should set hasUpdate to true when SwUpdate fires VERSION_READY', () => {
    versionUpdates$.next({ type: 'VERSION_READY', currentVersion: { hash: 'v1' }, latestVersion: { hash: 'v2' } });
    expect(service.hasUpdate()).toBe(true);
  });

  it('should dismiss update when dismissUpdate is called', () => {
    service.hasUpdate.set(true);
    service.dismissUpdate();
    expect(service.hasUpdate()).toBe(false);
  });

  it('should call swUpdate.activateUpdate when applyUpdate is called', () => {
    service.applyUpdate();
    expect(mockSwUpdate.activateUpdate).toHaveBeenCalled();
  });

  it('should return false for promptInstall when deferred prompt is not set', async () => {
    const result = await service.promptInstall();
    expect(result).toBe(false);
  });

  it('should prompt user and return true if install accepted', async () => {
    const mockPromptEvent = {
      prompt: vi.fn(),
      userChoice: Promise.resolve({ outcome: 'accepted' })
    };

    // Inject mock prompt
    (service as any).deferredInstallPrompt = mockPromptEvent;
    service.canInstall.set(true);

    const outcome = await service.promptInstall();
    expect(mockPromptEvent.prompt).toHaveBeenCalled();
    expect(outcome).toBe(true);
    expect(service.isInstalled()).toBe(true);
    expect(service.canInstall()).toBe(false);
  });

  it('should handle online and offline window events', () => {
    window.dispatchEvent(new Event('offline'));
    expect(service.isOffline()).toBe(true);

    window.dispatchEvent(new Event('online'));
    expect(service.isOffline()).toBe(false);
  });
});
