import { Injectable, inject, PLATFORM_ID, signal, OnDestroy } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { COOKIE_CONSENT_KEY } from '../constants/app.constants';
import { AnalyticsEvent, AnalyticsOverview } from '../models/analytics.models';

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  private readonly eventsApiUrl = '/api/v1/analytics/events';
  private readonly adminApiUrl = '/api/v1/admin/analytics/overview';

  readonly hasConsent = signal<boolean>(false);
  readonly isInitialized = signal<boolean>(false);

  private buffer: AnalyticsEvent[] = [];
  private flushTimer: any = null;
  private sessionId = '';

  constructor() {
    if (this.isBrowser) {
      this.initSession();
      this.checkConsent();
      this.setupUnloadListeners();
    }
  }

  ngOnDestroy(): void {
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
      this.flushTimer = null;
    }
  }

  private initSession(): void {
    try {
      let sid = sessionStorage.getItem('mc_analytics_sid');
      if (!sid) {
        sid = 's_' + Math.random().toString(36).substring(2, 11) + Date.now().toString(36);
        sessionStorage.setItem('mc_analytics_sid', sid);
      }
      this.sessionId = sid;
    } catch {
      this.sessionId = 's_' + Date.now().toString(36);
    }
  }

  private checkConsent(): void {
    const consent = localStorage.getItem(COOKIE_CONSENT_KEY);
    const granted = consent === 'accepted';
    this.hasConsent.set(granted);
    this.isInitialized.set(true);

    if (granted) {
      this.startPeriodicFlush();
    }
  }

  grantConsent(): void {
    if (!this.isBrowser) return;
    localStorage.setItem(COOKIE_CONSENT_KEY, 'accepted');
    this.hasConsent.set(true);
    this.startPeriodicFlush();
    this.trackEvent('consent_granted', 'analytics_enabled');
  }

  revokeConsent(): void {
    if (!this.isBrowser) return;
    localStorage.setItem(COOKIE_CONSENT_KEY, 'rejected');
    this.hasConsent.set(false);
    this.buffer = [];
    if (this.flushTimer) {
      clearInterval(this.flushTimer);
      this.flushTimer = null;
    }
  }

  private startPeriodicFlush(): void {
    if (this.flushTimer) return;
    this.flushTimer = setInterval(() => {
      this.flush();
    }, 15000); // Flush buffer every 15 seconds
  }

  private setupUnloadListeners(): void {
    window.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'hidden') {
        this.flush(true);
      }
    });

    window.addEventListener('pagehide', () => {
      this.flush(true);
    });
  }

  /**
   * Track title playback event
   */
  trackPlay(titleId: number, titleText: string, type: 'movie' | 'series'): void {
    this.trackEvent('plays', titleId.toString());
  }

  /**
   * Track search query event (and empty searches if 0 results)
   */
  trackSearch(query: string, resultCount: number): void {
    const cleanQuery = query.trim().toLowerCase();
    if (!cleanQuery) return;

    this.trackEvent('searches', cleanQuery);
    if (resultCount === 0) {
      this.trackEvent('empty_searches', cleanQuery);
    }
  }

  /**
   * Track video playback source failure
   */
  trackSourceFail(providerName: string, reason: string): void {
    this.trackEvent('source_fails', `${providerName}: ${reason}`);
  }

  /**
   * General event queueing (strictly gated by consent)
   */
  trackEvent(eventName: string, dimension?: string): void {
    if (!this.isBrowser || !this.hasConsent()) {
      return; // Strict zero-tracking if user has not granted consent
    }

    this.buffer.push({
      eventName,
      dimension,
      sessionId: this.sessionId,
      timestamp: new Date().toISOString()
    });

    // Auto-flush when batch reaches 10 items
    if (this.buffer.length >= 10) {
      this.flush();
    }
  }

  /**
   * Flush batched events to the backend
   */
  flush(useBeacon = false): void {
    if (!this.isBrowser || !this.hasConsent() || this.buffer.length === 0) {
      return;
    }

    const payload = this.buffer.splice(0, 50);

    if (useBeacon && typeof navigator !== 'undefined' && 'sendBeacon' in navigator) {
      try {
        const blob = new Blob([JSON.stringify({ events: payload })], {
          type: 'application/json'
        });
        const sent = navigator.sendBeacon(this.eventsApiUrl, blob);
        if (sent) return;
      } catch {
        // Fall back to standard HTTP call
      }
    }

    this.http.post(this.eventsApiUrl, { events: payload }).subscribe({
      error: (err) => {
        // On transient network error, re-queue up to 50 events without exceeding memory
        if (this.buffer.length < 100) {
          this.buffer.unshift(...payload);
        }
      }
    });
  }

  /**
   * Retrieve aggregated analytics overview (Admin only)
   */
  getAnalyticsOverview(startDate?: string, endDate?: string): Observable<AnalyticsOverview> {
    const params: Record<string, string> = {};
    if (startDate) params['startDate'] = startDate;
    if (endDate) params['endDate'] = endDate;

    return this.http.get<AnalyticsOverview>(this.adminApiUrl, { params });
  }
}
