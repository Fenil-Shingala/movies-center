import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PLATFORM_ID } from '@angular/core';
import { AuthService } from './auth.service';
import { GuestStorageService } from './guest-storage.service';
import { AuthResponse } from '../models/auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let httpTesting: HttpTestingController;
  let guestStorage: GuestStorageService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        GuestStorageService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });

    httpTesting = TestBed.inject(HttpTestingController);
    guestStorage = TestBed.inject(GuestStorageService);
    service = TestBed.inject(AuthService);

    // Initial silent refresh call from initializeAuth
    const initialReq = httpTesting.match('/api/v1/auth/refresh');
    if (initialReq.length > 0) {
      initialReq[0].error(new ProgressEvent('error'), { status: 401, statusText: 'Unauthorized' });
    }
  });

  afterEach(() => {
    httpTesting.verify();
    localStorage.clear();
  });

  it('should be created and default to unauthenticated', () => {
    expect(service).toBeTruthy();
    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
  });

  it('should authenticate user and clear guest storage on login', () => {
    guestStorage.addToWatchlist(42);
    expect(guestStorage.hasGuestData()).toBe(true);

    const mockResponse: AuthResponse = {
      accessToken: 'mock_jwt_token',
      expiresAt: new Date(Date.now() + 3600000).toISOString(),
      user: {
        id: '12345678-1234-1234-1234-123456789abc',
        email: 'cinema@fan.com',
        displayName: 'Cinema Fan',
        roles: ['User']
      }
    };

    service.login('cinema@fan.com', 'Password123!').subscribe(res => {
      expect(res.accessToken).toBe('mock_jwt_token');
      expect(service.isAuthenticated()).toBe(true);
      expect(service.currentUser()?.displayName).toBe('Cinema Fan');
      expect(guestStorage.hasGuestData()).toBe(false); // Merged & cleared
    });

    const req = httpTesting.expectOne('/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.guestData).toBeTruthy();
    expect(req.request.body.guestData.watchlistTitleIds).toContain(42);
    req.flush(mockResponse);
  });

  it('should clear user state on logout', () => {
    service.token.set('existing_token');
    service.currentUser.set({
      id: '123',
      email: 'user@test.com',
      displayName: 'User',
      roles: ['User']
    });

    expect(service.isAuthenticated()).toBe(true);

    service.logout().subscribe();

    const req = httpTesting.expectOne('/api/v1/auth/logout');
    expect(req.request.method).toBe('POST');
    req.flush({ message: 'Logged out' });

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(service.token()).toBeNull();
  });

  it('should identify admin users accurately', () => {
    service.currentUser.set({
      id: 'admin_id',
      email: 'admin@moviescenter.com',
      displayName: 'Admin User',
      roles: ['Admin', 'User']
    });
    service.token.set('admin_token');

    expect(service.isAdmin()).toBe(true);
  });
});
