import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { NotificationService } from './notification.service';
import { AuthService } from './auth.service';
import { NotificationItem, NotificationSettings, PagedNotificationsResponse } from '../models/notification.models';

describe('NotificationService', () => {
  let service: NotificationService;
  let httpMock: HttpTestingController;
  let isAuthSignal = signal<boolean>(false);

  beforeEach(() => {
    isAuthSignal.set(false);

    TestBed.configureTestingModule({
      providers: [
        NotificationService,
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: isAuthSignal,
            currentUser: signal({ id: '123', email: 'test@user.com', displayName: 'Test User' })
          }
        }
      ]
    });

    service = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    TestBed.resetTestingModule();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should load notifications and update signal', () => {
    isAuthSignal.set(true);

    const mockResponse: PagedNotificationsResponse = {
      items: [
        {
          id: 'n1',
          type: 'NewEpisode',
          title: 'New Episode Available',
          body: 'S01E02 is out now',
          data: null,
          isRead: false,
          createdAt: '2026-10-28T12:00:00Z'
        }
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1
    };

    service.loadNotifications().subscribe(res => {
      expect(res).toBeTruthy();
      expect(service.notifications().length).toBe(1);
      expect(service.notifications()[0].title).toBe('New Episode Available');
    });

    const req = httpMock.expectOne(r => r.url.endsWith('/api/v1/me/notifications'));
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should load unread count and update signal', () => {
    isAuthSignal.set(true);

    service.loadUnreadCount().subscribe(res => {
      expect(res?.unreadCount).toBe(4);
      expect(service.unreadCount()).toBe(4);
      expect(service.hasUnread()).toBe(true);
    });

    const req = httpMock.expectOne(r => r.url.endsWith('/api/v1/me/notifications/unread-count'));
    expect(req.request.method).toBe('GET');
    req.flush({ unreadCount: 4 });
  });

  it('should optimistically mark notification as read and decrement unreadCount', () => {
    isAuthSignal.set(true);

    const item: NotificationItem = {
      id: 'n-123',
      type: 'NewEpisode',
      title: 'Alert',
      body: 'Content',
      data: null,
      isRead: false,
      createdAt: '2026-10-28T00:00:00Z'
    };

    service.notifications.set([item]);
    service.unreadCount.set(1);

    service.markAsRead(item).subscribe();

    expect(service.notifications()[0].isRead).toBe(true);
    expect(service.unreadCount()).toBe(0);

    const req = httpMock.expectOne(r => r.url.endsWith('/api/v1/me/notifications/n-123/read'));
    expect(req.request.method).toBe('POST');
    req.flush({ success: true });
  });

  it('should optimistically mark all notifications as read and reset unreadCount', () => {
    isAuthSignal.set(true);

    service.notifications.set([
      { id: '1', type: 'type', title: 'T1', body: 'B', data: null, isRead: false, createdAt: '' },
      { id: '2', type: 'type', title: 'T2', body: 'B', data: null, isRead: false, createdAt: '' }
    ]);
    service.unreadCount.set(2);

    service.markAllAsRead().subscribe();

    expect(service.notifications().every(n => n.isRead)).toBe(true);
    expect(service.unreadCount()).toBe(0);

    const req = httpMock.expectOne(r => r.url.endsWith('/api/v1/me/notifications/read-all'));
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, markedCount: 2 });
  });

  it('should load settings and update settings signal', () => {
    isAuthSignal.set(true);

    const mockSettings: NotificationSettings = {
      newEpisodeInApp: true,
      newEpisodeEmail: true,
      newEpisodePush: false,
      continueWatchingInApp: true,
      continueWatchingEmail: false,
      continueWatchingPush: false
    };

    service.loadSettings().subscribe(res => {
      expect(res).toEqual(mockSettings);
      expect(service.settings()).toEqual(mockSettings);
    });

    const req = httpMock.expectOne(r => r.url.endsWith('/api/v1/me/notification-settings'));
    expect(req.request.method).toBe('GET');
    req.flush(mockSettings);
  });
});
