import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { adminGuard } from './admin.guard';
import { AuthService } from '../services/auth.service';

describe('adminGuard', () => {
  let authServiceMock: { isAdmin: () => boolean; openAuthModal: (mode: string) => void };
  let routerMock: { createUrlTree: (url: string[]) => any };

  beforeEach(() => {
    authServiceMock = {
      isAdmin: () => false,
      openAuthModal: () => {}
    };

    routerMock = {
      createUrlTree: (url: string[]) => url.join('/')
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    });
  });

  it('should allow access if user is Admin', () => {
    authServiceMock.isAdmin = () => true;

    const result = TestBed.runInInjectionContext(() => adminGuard({} as any, {} as any));
    expect(result).toBe(true);
  });

  it('should redirect to home and open auth modal if not Admin', () => {
    authServiceMock.isAdmin = () => false;

    let openedMode = '';
    authServiceMock.openAuthModal = (m: string) => { openedMode = m; };

    const result = TestBed.runInInjectionContext(() => adminGuard({} as any, {} as any));
    expect(result).toBe('/');
    expect(openedMode).toBe('login');
  });

  it('should wait for initialization observable before granting access to Admin', async () => {
    const { BehaviorSubject, firstValueFrom } = await import('rxjs');
    const initSubject = new BehaviorSubject<boolean>(false);
    authServiceMock.isAdmin = () => false;
    (authServiceMock as any).isInitialized = () => initSubject.value;
    (authServiceMock as any).isInitialized$ = initSubject.asObservable();

    const result$ = TestBed.runInInjectionContext(() => adminGuard({} as any, {} as any)) as any;
    expect(result$.subscribe).toBeDefined();

    // Now complete initialization and set admin
    authServiceMock.isAdmin = () => true;
    initSubject.next(true);

    const val = await firstValueFrom(result$);
    expect(val).toBe(true);
  });
});
