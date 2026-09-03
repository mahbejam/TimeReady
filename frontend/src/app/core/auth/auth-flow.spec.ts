import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthResponse } from '../models/auth.model';
import { authInterceptor } from './auth.interceptor';
import { authGuard, guestGuard, roleGuard } from './auth.guard';
import { AuthService } from './auth.service';

/**
 * Walks the whole signed-in lifecycle with the real service, the real
 * interceptor and the real guards wired together. Only the network is faked,
 * and the payloads are shaped exactly like the ones the API returns.
 */
describe('authentication flow', () => {
  const state = { url: '/employees' } as RouterStateSnapshot;
  const route = {} as ActivatedRouteSnapshot;

  let auth: AuthService;
  let http: HttpClient;
  let controller: HttpTestingController;
  let router: Router;

  /** Exactly the JSON the API returns from /api/auth/login and /api/auth/refresh. */
  function authResponse(
    accessToken: string,
    refreshToken: string,
    roles: string[] = ['Admin']
  ): AuthResponse {
    return {
      accessToken,
      expiresAtUtc: '2026-07-24T15:00:00+00:00',
      refreshToken,
      refreshTokenExpiresAtUtc: '2026-07-31T14:30:00+00:00',
      user: {
        id: 'b1f0c6f2-0c6a-4a1c-9f2e-1d3c5a7b9e11',
        email: roles.includes('Admin') ? 'admin@timeready.local' : 'operator@timeready.local',
        fullName: roles.includes('Admin') ? 'TimeReady Administrator' : 'HR Operator',
        roles: roles as AuthResponse['user']['roles']
      }
    };
  }

  function signIn(roles: string[] = ['Admin']): void {
    auth.login({ email: 'admin@timeready.local', password: 'Admin#Demo2026' }).subscribe();
    controller
      .expectOne(request => request.url.endsWith('/auth/login'))
      .flush(authResponse('access-1', 'refresh-1', roles));
  }

  function run<T>(guard: () => T): T {
    return TestBed.runInInjectionContext(guard);
  }

  beforeEach(() => {
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });

    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);

    // The interceptor navigates on a failed refresh; the assertions read the spy
    // off the router itself, which keeps the typing honest.
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    controller.verify();
    sessionStorage.clear();
  });

  it('signs in, guards open up, and API calls carry the token', () => {
    // Before signing in, a protected route sends the visitor to the login page.
    const blocked = run(() => authGuard(route, state)) as UrlTree;

    expect(router.serializeUrl(blocked)).toBe('/login?returnUrl=%2Femployees');

    signIn();

    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.user()?.fullName).toBe('TimeReady Administrator');
    expect(run(() => authGuard(route, state))).toBe(true);
    expect(router.serializeUrl(run(() => guestGuard(route, state)) as UrlTree)).toBe('/dashboard');

    http.get('/api/employees').subscribe();

    const employees = controller.expectOne('/api/employees');

    expect(employees.request.headers.get('Authorization')).toBe('Bearer access-1');
    employees.flush([]);
  });

  it('recovers from an expired access token without the caller noticing', () => {
    signIn();

    let received: unknown = null;

    http.get('/api/employees').subscribe(result => (received = result));

    controller
      .expectOne('/api/employees')
      .flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    const refresh = controller.expectOne(request => request.url.endsWith('/auth/refresh'));

    expect(refresh.request.body).toEqual({ refreshToken: 'refresh-1' });
    refresh.flush(authResponse('access-2', 'refresh-2'));

    const replay = controller.expectOne('/api/employees');

    expect(replay.request.headers.get('Authorization')).toBe('Bearer access-2');
    replay.flush([{ id: 1, fullName: 'Anna Gruber' }]);

    expect(received).toEqual([{ id: 1, fullName: 'Anna Gruber' }]);
    // The rotated refresh token replaced the old one.
    expect(sessionStorage.getItem('timeready.refresh-token')).toBe('refresh-2');
  });

  it('refreshes once when several requests hit a 401 together', () => {
    signIn();

    http.get('/api/employees').subscribe();
    http.get('/api/readiness').subscribe();

    controller
      .expectOne('/api/employees')
      .flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    controller
      .expectOne('/api/readiness')
      .flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    // One refresh for both failures, not two.
    controller
      .expectOne(request => request.url.endsWith('/auth/refresh'))
      .flush(authResponse('access-2', 'refresh-2'));

    controller.expectOne('/api/employees').flush([]);
    controller.expectOne('/api/readiness').flush([]);
  });

  it('ends the session and returns to the login page when the refresh is refused', () => {
    signIn();

    http.get('/api/employees').subscribe({ error: () => undefined });

    controller
      .expectOne('/api/employees')
      .flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    controller
      .expectOne(request => request.url.endsWith('/auth/refresh'))
      .flush({ title: 'Sign in failed.' }, { status: 401, statusText: 'Unauthorized' });

    expect(auth.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem('timeready.refresh-token')).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: router.url } });
    expect(run(() => authGuard(route, state))).toBeInstanceOf(UrlTree);
  });

  it('gives an administrator the audit trail and keeps an operator out', () => {
    signIn(['Admin']);

    expect(auth.isAdmin()).toBe(true);
    expect(run(() => roleGuard('Admin')(route, state))).toBe(true);

    http.get('/api/audit?page=1&pageSize=25').subscribe();

    const audit = controller.expectOne('/api/audit?page=1&pageSize=25');

    expect(audit.request.headers.get('Authorization')).toBe('Bearer access-1');
    audit.flush({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0, hasNextPage: false });

    auth.clearSession();
    signIn(['Operator']);

    expect(auth.isAdmin()).toBe(false);
    expect(run(() => authGuard(route, state))).toBe(true);
    expect(router.serializeUrl(run(() => roleGuard('Admin')(route, state)) as UrlTree)).toBe('/no-access');
  });

  it('revokes the refresh token on sign out and closes the guards again', () => {
    signIn();

    auth.logout().subscribe();

    const logout = controller.expectOne(request => request.url.endsWith('/auth/logout'));

    expect(logout.request.headers.get('Authorization')).toBe('Bearer access-1');
    expect(logout.request.body).toEqual({ refreshToken: 'refresh-1' });
    logout.flush(null);

    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.accessToken).toBeNull();
    expect(sessionStorage.getItem('timeready.refresh-token')).toBeNull();
    expect(run(() => authGuard(route, state))).toBeInstanceOf(UrlTree);
  });

  it('restores the session after a page reload', async () => {
    // What a reload looks like: the refresh token survived, the access token did not.
    sessionStorage.setItem('timeready.refresh-token', 'refresh-from-earlier');

    const restored = auth.restoreSession();

    const refresh = controller.expectOne(request => request.url.endsWith('/auth/refresh'));

    expect(refresh.request.body).toEqual({ refreshToken: 'refresh-from-earlier' });
    refresh.flush(authResponse('access-9', 'refresh-9'));

    await restored;

    expect(auth.isAuthenticated()).toBe(true);
    expect(run(() => authGuard(route, state))).toBe(true);
  });

  it('starts signed out when a reload finds an expired refresh token', async () => {
    sessionStorage.setItem('timeready.refresh-token', 'expired');

    const restored = auth.restoreSession();

    controller
      .expectOne(request => request.url.endsWith('/auth/refresh'))
      .flush({ title: 'Sign in failed.' }, { status: 401, statusText: 'Unauthorized' });

    await restored;

    expect(auth.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem('timeready.refresh-token')).toBeNull();
  });
});
