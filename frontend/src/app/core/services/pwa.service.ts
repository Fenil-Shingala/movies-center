import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { Observable, catchError, filter, of, tap } from 'rxjs';
import { AuthService } from './auth.service';

export interface VapidPublicKeyResponse {
  publicKey: string;
}

@Injectable({
  providedIn: 'root'
})
export class PwaService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly swUpdate = inject(SwUpdate, { optional: true });

  private readonly baseUrl = '/api/v1/me/push-subscriptions';

  // Reactive State Signals
  readonly canInstall = signal<boolean>(false);
  readonly isInstalled = signal<boolean>(false);
  readonly hasUpdate = signal<boolean>(false);
  readonly isOffline = signal<boolean>(false);
  readonly isPushSubscribed = signal<boolean>(false);
  readonly isSubscribingPush = signal<boolean>(false);

  private deferredInstallPrompt: any = null;

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      this.initInstallPrompt();
      this.initNetworkStatus();
      this.initUpdateChecker();
      this.checkInitialPushStatus();
    }
  }

  private initInstallPrompt(): void {
    window.addEventListener('beforeinstallprompt', (e: Event) => {
      // Prevent browser default mini-infobar
      e.preventDefault();
      this.deferredInstallPrompt = e;
      this.canInstall.set(true);
    });

    window.addEventListener('appinstalled', () => {
      this.canInstall.set(false);
      this.isInstalled.set(true);
      this.deferredInstallPrompt = null;
    });

    // Detect standalone display mode safely
    try {
      if (typeof window !== 'undefined' && typeof window.matchMedia === 'function' && window.matchMedia('(display-mode: standalone)').matches) {
        this.isInstalled.set(true);
      } else if (typeof window !== 'undefined' && (window.navigator as any)?.standalone) {
        this.isInstalled.set(true);
      }
    } catch {
      // Safe fallback for environments without matchMedia
    }
  }

  private initNetworkStatus(): void {
    if (typeof navigator !== 'undefined') {
      this.isOffline.set(!navigator.onLine);
    }

    window.addEventListener('online', () => {
      this.isOffline.set(false);
    });

    window.addEventListener('offline', () => {
      this.isOffline.set(true);
    });
  }

  private initUpdateChecker(): void {
    if (!this.swUpdate || !this.swUpdate.isEnabled) return;

    this.swUpdate.versionUpdates
      .pipe(filter((evt): evt is VersionReadyEvent => evt.type === 'VERSION_READY'))
      .subscribe(() => {
        this.hasUpdate.set(true);
      });
  }

  private async checkInitialPushStatus(): Promise<void> {
    if (typeof window !== 'undefined' && 'serviceWorker' in navigator && navigator.serviceWorker?.ready && 'PushManager' in window) {
      try {
        const registration = await navigator.serviceWorker.ready;
        if (registration?.pushManager) {
          const sub = await registration.pushManager.getSubscription();
          this.isPushSubscribed.set(!!sub);
        }
      } catch (err) {
        console.warn('Could not check initial push status', err);
      }
    }
  }

  async promptInstall(): Promise<boolean> {
    if (!this.deferredInstallPrompt) return false;

    this.deferredInstallPrompt.prompt();
    const { outcome } = await this.deferredInstallPrompt.userChoice;
    this.deferredInstallPrompt = null;
    this.canInstall.set(false);

    if (outcome === 'accepted') {
      this.isInstalled.set(true);
      return true;
    }
    return false;
  }

  applyUpdate(): void {
    if (!this.swUpdate || !this.swUpdate.isEnabled) {
      window.location.reload();
      return;
    }

    this.swUpdate.activateUpdate().then(() => {
      window.location.reload();
    });
  }

  dismissUpdate(): void {
    this.hasUpdate.set(false);
  }

  async subscribeToPush(): Promise<boolean> {
    if (!isPlatformBrowser(this.platformId)) return false;
    if (!('Notification' in window) || !('serviceWorker' in navigator)) {
      alert('Push notifications are not supported in this browser.');
      return false;
    }

    this.isSubscribingPush.set(true);

    try {
      const permission = await Notification.requestPermission();
      if (permission !== 'granted') {
        this.isSubscribingPush.set(false);
        return false;
      }

      // Fetch VAPID public key from backend
      const keyRes = await this.http.get<VapidPublicKeyResponse>(`${this.baseUrl}/vapid-public-key`).toPromise();
      if (!keyRes?.publicKey) {
        throw new Error('VAPID public key not provided by backend.');
      }

      const registration = await navigator.serviceWorker.ready;
      let subscription = await registration.pushManager.getSubscription();

      if (!subscription) {
        const convertedKey = this.urlBase64ToUint8Array(keyRes.publicKey);
        subscription = await registration.pushManager.subscribe({
          userVisibleOnly: true,
          applicationServerKey: convertedKey as unknown as BufferSource
        });
      }

      // Serialize and send subscription to backend
      const rawSub = subscription.toJSON();
      const p256dh = rawSub.keys?.['p256dh'] || '';
      const auth = rawSub.keys?.['auth'] || '';

      await this.http.post(this.baseUrl, {
        endpoint: subscription.endpoint,
        p256dh,
        auth,
        userAgent: navigator.userAgent
      }).toPromise();

      this.isPushSubscribed.set(true);
      this.isSubscribingPush.set(false);
      return true;
    } catch (err) {
      console.error('Failed to subscribe to Web Push', err);
      this.isSubscribingPush.set(false);
      return false;
    }
  }

  async unsubscribeFromPush(): Promise<boolean> {
    if (!isPlatformBrowser(this.platformId)) return false;

    try {
      const registration = await navigator.serviceWorker.ready;
      const subscription = await registration.pushManager.getSubscription();

      if (subscription) {
        const endpoint = subscription.endpoint;
        await subscription.unsubscribe();

        // Inform backend
        await this.http.request('delete', this.baseUrl, {
          body: { endpoint }
        }).toPromise();
      }

      this.isPushSubscribed.set(false);
      return true;
    } catch (err) {
      console.error('Failed to unsubscribe from Web Push', err);
      return false;
    }
  }

  private urlBase64ToUint8Array(base64String: string): Uint8Array {
    const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
    const base64 = (base64String + padding)
      .replace(/-/g, '+')
      .replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
      outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
  }
}
