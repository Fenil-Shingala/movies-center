import { Injectable, signal, computed, inject, PLATFORM_ID } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { Observable, tap, catchError, of, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { 
  AuthResponse, 
  UserProfile, 
  LoginRequest, 
  RegisterRequest, 
  GuestMergeResult,
  ImportGuestDataRequest 
} from '../models/auth.models';
import { GuestStorageService } from './guest-storage.service';

const TOKEN_KEY = 'mc_access_token';
const USER_KEY = 'mc_user_profile';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly guestStorage = inject(GuestStorageService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  private readonly apiUrl = `${environment.apiUrl}/v1`;

  readonly currentUser = signal<UserProfile | null>(null);
  readonly token = signal<string | null>(null);
  readonly isInitialized = signal<boolean>(false);
  readonly isInitialized$ = toObservable(this.isInitialized);
  readonly isAuthModalOpen = signal<boolean>(false);
  readonly authModalMode = signal<'login' | 'register'>('login');

  readonly isAuthenticated = computed(() => !!this.currentUser() && !!this.token());
  readonly isAdmin = computed(() => this.currentUser()?.roles.includes('Admin') ?? false);

  constructor() {
    this.initializeAuth();
  }

  private initializeAuth(): void {
    if (!this.isBrowser) {
      this.isInitialized.set(true);
      return;
    }

    const savedToken = localStorage.getItem(TOKEN_KEY);
    const savedUser = localStorage.getItem(USER_KEY);

    if (savedToken) {
      this.token.set(savedToken);
      if (savedUser) {
        try {
          this.currentUser.set(JSON.parse(savedUser));
        } catch {
          localStorage.removeItem(USER_KEY);
        }
      }

      this.fetchCurrentUser().subscribe({
        next: () => this.isInitialized.set(true),
        error: () => {
          // Token expired or invalid, try silent refresh
          this.refreshToken().subscribe({
            next: () => this.isInitialized.set(true),
            error: () => {
              this.clearSession();
              this.isInitialized.set(true);
            }
          });
        }
      });
    } else {
      // Try silent refresh from HTTP-only cookie if available
      this.refreshToken().subscribe({
        next: () => this.isInitialized.set(true),
        error: () => this.isInitialized.set(true)
      });
    }
  }

  login(email: string, password: string): Observable<AuthResponse> {
    const guestData = this.guestStorage.hasGuestData() ? this.guestStorage.getGuestData() : null;
    const payload: LoginRequest = { email, password, guestData };

    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, payload, {
      withCredentials: true
    }).pipe(
      tap(res => {
        this.handleAuthSuccess(res);
        if (guestData) {
          this.guestStorage.clearGuestData();
        }
      })
    );
  }

  register(email: string, password: string, displayName: string): Observable<AuthResponse> {
    const guestData = this.guestStorage.hasGuestData() ? this.guestStorage.getGuestData() : null;
    const payload: RegisterRequest = { email, password, displayName, guestData };

    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/register`, payload, {
      withCredentials: true
    }).pipe(
      tap(res => {
        this.handleAuthSuccess(res);
        if (guestData) {
          this.guestStorage.clearGuestData();
        }
      })
    );
  }

  refreshToken(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/refresh`, {}, {
      withCredentials: true
    }).pipe(
      tap(res => this.handleAuthSuccess(res)),
      catchError(err => {
        this.clearSession();
        return throwError(() => err);
      })
    );
  }

  logout(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/auth/logout`, {}, {
      withCredentials: true
    }).pipe(
      tap(() => this.clearSession()),
      catchError(() => {
        this.clearSession();
        return of({ message: 'Logged out' });
      })
    );
  }

  fetchCurrentUser(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.apiUrl}/auth/me`).pipe(
      tap(user => {
        this.currentUser.set(user);
        if (this.isBrowser) {
          localStorage.setItem(USER_KEY, JSON.stringify(user));
        }
      })
    );
  }

  importGuestData(): Observable<GuestMergeResult> {
    const guestData = this.guestStorage.getGuestData();
    const payload: ImportGuestDataRequest = {
      watchlistTitleIds: guestData.watchlistTitleIds,
      history: guestData.history
    };

    return this.http.post<GuestMergeResult>(`${this.apiUrl}/me/import-guest-data`, payload).pipe(
      tap(() => this.guestStorage.clearGuestData())
    );
  }

  openAuthModal(mode: 'login' | 'register' = 'login'): void {
    this.authModalMode.set(mode);
    this.isAuthModalOpen.set(true);
  }

  closeAuthModal(): void {
    this.isAuthModalOpen.set(false);
  }

  private handleAuthSuccess(res: AuthResponse): void {
    this.token.set(res.accessToken);
    this.currentUser.set(res.user);
    if (this.isBrowser) {
      localStorage.setItem(TOKEN_KEY, res.accessToken);
      localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    }
    this.closeAuthModal();
  }

  private clearSession(): void {
    this.token.set(null);
    this.currentUser.set(null);
    if (this.isBrowser) {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
    }
  }
}
