import { Injectable, computed, inject, signal, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, catchError, of, tap } from 'rxjs';
import { 
  NotificationItem, 
  NotificationSettings, 
  PagedNotificationsResponse, 
  UnreadCountResponse 
} from '../models/notification.models';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class NotificationService implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  private readonly baseUrl = '/api/v1/me';

  // Reactive State Signals
  readonly notifications = signal<NotificationItem[]>([]);
  readonly unreadCount = signal<number>(0);
  readonly settings = signal<NotificationSettings | null>(null);
  readonly isLoading = signal<boolean>(false);
  readonly isSavingSettings = signal<boolean>(false);
  readonly isOpen = signal<boolean>(false);
  readonly isSettingsModalOpen = signal<boolean>(false);

  readonly hasUnread = computed(() => this.unreadCount() > 0);

  private pollInterval: any = null;

  constructor() {
    // Initial sync and reactive polling
    if (this.authService.isAuthenticated()) {
      this.refreshAll();
    }

    // Set up light polling every 60 seconds when user is authenticated
    if (typeof window !== 'undefined') {
      this.pollInterval = setInterval(() => {
        if (this.authService.isAuthenticated()) {
          this.loadUnreadCount().subscribe();
        }
      }, 60000);
    }
  }

  ngOnDestroy(): void {
    if (this.pollInterval) {
      clearInterval(this.pollInterval);
      this.pollInterval = null;
    }
  }

  refreshAll(): void {
    if (!this.authService.isAuthenticated()) return;
    this.loadUnreadCount().subscribe();
    this.loadNotifications().subscribe();
    this.loadSettings().subscribe();
  }

  toggleDropdown(): void {
    const nextState = !this.isOpen();
    this.isOpen.set(nextState);
    if (nextState) {
      this.loadNotifications().subscribe();
      this.loadUnreadCount().subscribe();
    }
  }

  closeDropdown(): void {
    this.isOpen.set(false);
  }

  openSettingsModal(): void {
    this.isSettingsModalOpen.set(true);
    this.closeDropdown();
    if (!this.settings()) {
      this.loadSettings().subscribe();
    }
  }

  closeSettingsModal(): void {
    this.isSettingsModalOpen.set(false);
  }

  loadNotifications(page = 1, pageSize = 20): Observable<PagedNotificationsResponse | null> {
    if (!this.authService.isAuthenticated()) return of(null);

    this.isLoading.set(true);
    return this.http.get<PagedNotificationsResponse>(`${this.baseUrl}/notifications`, {
      params: { page, pageSize }
    }).pipe(
      tap(res => {
        this.notifications.set(res.items);
        this.isLoading.set(false);
      }),
      catchError(err => {
        console.error('Failed to load notifications', err);
        this.isLoading.set(false);
        return of(null);
      })
    );
  }

  loadUnreadCount(): Observable<UnreadCountResponse | null> {
    if (!this.authService.isAuthenticated()) return of(null);

    return this.http.get<UnreadCountResponse>(`${this.baseUrl}/notifications/unread-count`).pipe(
      tap(res => {
        this.unreadCount.set(res.unreadCount);
      }),
      catchError(err => {
        console.error('Failed to load unread notification count', err);
        return of(null);
      })
    );
  }

  markAsRead(item: NotificationItem): Observable<any> {
    if (item.isRead) return of(null);

    // Optimistic UI update
    this.notifications.update(items =>
      items.map(n => n.id === item.id ? { ...n, isRead: true } : n)
    );
    this.unreadCount.update(c => Math.max(0, c - 1));

    return this.http.post(`${this.baseUrl}/notifications/${item.id}/read`, {}).pipe(
      catchError(err => {
        console.error(`Failed to mark notification ${item.id} as read`, err);
        return of(null);
      })
    );
  }

  markAllAsRead(): Observable<any> {
    // Optimistic UI update
    this.notifications.update(items =>
      items.map(n => ({ ...n, isRead: true }))
    );
    this.unreadCount.set(0);

    return this.http.post(`${this.baseUrl}/notifications/read-all`, {}).pipe(
      catchError(err => {
        console.error('Failed to mark all notifications as read', err);
        return of(null);
      })
    );
  }

  deleteNotification(id: string): Observable<any> {
    const wasUnread = this.notifications().find(n => n.id === id && !n.isRead);

    // Optimistic UI removal
    this.notifications.update(items => items.filter(n => n.id !== id));
    if (wasUnread) {
      this.unreadCount.update(c => Math.max(0, c - 1));
    }

    return this.http.delete(`${this.baseUrl}/notifications/${id}`).pipe(
      catchError(err => {
        console.error(`Failed to delete notification ${id}`, err);
        return of(null);
      })
    );
  }

  loadSettings(): Observable<NotificationSettings | null> {
    if (!this.authService.isAuthenticated()) return of(null);

    return this.http.get<NotificationSettings>(`${this.baseUrl}/notification-settings`).pipe(
      tap(res => {
        this.settings.set(res);
      }),
      catchError(err => {
        console.error('Failed to load notification settings', err);
        return of(null);
      })
    );
  }

  updateSettings(dto: NotificationSettings): Observable<NotificationSettings | null> {
    this.isSavingSettings.set(true);

    return this.http.put<NotificationSettings>(`${this.baseUrl}/notification-settings`, dto).pipe(
      tap(res => {
        this.settings.set(res);
        this.isSavingSettings.set(false);
      }),
      catchError(err => {
        console.error('Failed to update notification settings', err);
        this.isSavingSettings.set(false);
        return of(null);
      })
    );
  }

  sendTestNotification(type: 'NewEpisode' | 'ContinueWatchingReminder' = 'NewEpisode'): Observable<NotificationItem | null> {
    return this.http.post<NotificationItem>(`${this.baseUrl}/notifications/test-seed`, {}, {
      params: { type }
    }).pipe(
      tap(newNotif => {
        if (newNotif) {
          this.notifications.update(existing => [newNotif, ...existing]);
          this.unreadCount.update(c => c + 1);
        }
      }),
      catchError(err => {
        console.error('Failed to send test notification', err);
        return of(null);
      })
    );
  }

  handleNotificationClick(item: NotificationItem): void {
    this.markAsRead(item).subscribe();
    this.closeDropdown();

    if (!item.data) return;

    try {
      const parsed = JSON.parse(item.data);
      if (parsed.actionUrl) {
        this.router.navigateByUrl(parsed.actionUrl);
      } else if (parsed.titleId && parsed.type) {
        this.router.navigate(['/watch', parsed.type, parsed.titleId]);
      }
    } catch {
      // Non-JSON data or plain string
      if (item.data.startsWith('/')) {
        this.router.navigateByUrl(item.data);
      }
    }
  }

  formatTimeAgo(dateString: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

    if (seconds < 60) return 'Just now';
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    if (days < 7) return `${days}d ago`;
    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }
}
