import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { AuthResponse } from '../models/auth.model';
import { AuthService } from './auth.service';
import { TokenStorage } from './token-storage';

function response(overrides: Partial<AuthResponse> = {}): AuthResponse {
  return {
    accessToken: 'access-1',
    expiresAtUtc: '2026-07-24T13:00:00Z',
    refreshToken: 'refresh-1',
    refreshTokenExpiresAtUtc: '2026-07-31T12:00:00Z',
    user: {
      id: 'user-1',
      email: 'admin@timeready.local',
      fullName: 'TimeReady Administrator',
      roles: ['Admin']
    },
    ...overrides
  };
}

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;
  let storage: TokenStorage;

  beforeEach(() => {
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
    storage = TestBed.inject(TokenStorage);
  });

  it('starts signed out', () => {
    expect(service.isAuthenticated()).toBe(false);
    expect(service.accessToken).toBeNull();
    expect(service.roles()).toEqual([]);
  });

  it('keeps the user and the access token after a successful login', () => {
    service.login({ email: 'admin@timeready.local', password: 'secret' }).subscribe();

    const request = http.expectOne(req => req.url.endsWith('/auth/login'));

    expect(request.request.body).toEqual({ email: 'admin@timeready.local', password: 'secret' });
    request.flush(response());

    expect(service.isAuthenticated()).toBe(true);
    expect(service.accessToken).toBe('access-1');
    expect(service.user()?.email).toBe('admin@timeready.local');
    expect(service.isAdmin()).toBe(true);
  });

  it('persists only the refresh token, never the access token', () => {
    service.login({ email: 'admin@timeready.local', password: 'secret' }).subscribe();
    http.expectOne(req => req.url.endsWith('/auth/login')).flush(response());

    expect(storage.refreshToken).toBe('refresh-1');
    expect(sessionStorage.getItem('timeready.refresh-token')).toBe('refresh-1');
    expect(JSON.stringify(sessionStorage)).not.toContain('access-1');
  });

  it('leaves the session untouched when the login fails', () => {
    let failed = false;

    service.login({ email: 'admin@timeready.local', password: 'wrong' }).subscribe({
      error: () => (failed = true)
    });

    http
      .expectOne(req => req.url.endsWith('/auth/login'))
      .flush({ title: 'Sign in failed.' }, { status: 401, statusText: 'Unauthorized' });

    expect(failed).toBe(true);
    expect(service.isAuthenticated()).toBe(false);
    expect(storage.refreshToken).toBeNull();
  });

  it('sends one refresh request even when several callers ask at once', () => {
    storage.refreshToken = 'refresh-1';

    const tokens: string[] = [];

    service.refreshTokens().subscribe(token => tokens.push(token));
    service.refreshTokens().subscribe(token => tokens.push(token));

    http.expectOne(req => req.url.endsWith('/auth/refresh')).flush(response({ accessToken: 'access-2' }));

    expect(tokens).toEqual(['access-2', 'access-2']);
    expect(service.accessToken).toBe('access-2');
  });

  it('clears the session when the refresh is rejected', () => {
    storage.refreshToken = 'expired';

    let failed = false;

    service.refreshTokens().subscribe({ error: () => (failed = true) });

    http
      .expectOne(req => req.url.endsWith('/auth/refresh'))
      .flush({ title: 'Sign in failed.' }, { status: 401, statusText: 'Unauthorized' });

    expect(failed).toBe(true);
    expect(service.isAuthenticated()).toBe(false);
    expect(storage.refreshToken).toBeNull();
  });

  it('revokes the refresh token on logout and clears the session', () => {
    service.login({ email: 'admin@timeready.local', password: 'secret' }).subscribe();
    http.expectOne(req => req.url.endsWith('/auth/login')).flush(response());

    service.logout().subscribe();

    const request = http.expectOne(req => req.url.endsWith('/auth/logout'));

    expect(request.request.body).toEqual({ refreshToken: 'refresh-1' });
    request.flush(null);

    expect(service.isAuthenticated()).toBe(false);
    expect(service.accessToken).toBeNull();
    expect(storage.refreshToken).toBeNull();
  });

  it('still signs the user out locally when the revoke call fails', () => {
    service.login({ email: 'admin@timeready.local', password: 'secret' }).subscribe();
    http.expectOne(req => req.url.endsWith('/auth/login')).flush(response());

    service.logout().subscribe();
    http
      .expectOne(req => req.url.endsWith('/auth/logout'))
      .flush(null, { status: 500, statusText: 'Server Error' });

    expect(service.isAuthenticated()).toBe(false);
    expect(storage.refreshToken).toBeNull();
  });

  it('restores a session from the stored refresh token', async () => {
    storage.refreshToken = 'refresh-1';

    const restored = service.restoreSession();

    http.expectOne(req => req.url.endsWith('/auth/refresh')).flush(response({ accessToken: 'access-3' }));
    await restored;

    expect(service.isAuthenticated()).toBe(true);
    expect(service.accessToken).toBe('access-3');
  });

  it('does not call the API when there is nothing to restore', async () => {
    await service.restoreSession();

    http.expectNone(() => true);
    expect(service.isAuthenticated()).toBe(false);
  });

  it('answers role questions from the current user', () => {
    service.login({ email: 'operator@timeready.local', password: 'secret' }).subscribe();
    http.expectOne(req => req.url.endsWith('/auth/login')).flush(
      response({
        user: {
          id: 'user-2',
          email: 'operator@timeready.local',
          fullName: 'HR Operator',
          roles: ['Operator']
        }
      })
    );

    expect(service.isAdmin()).toBe(false);
    expect(service.hasAnyRole(['Admin'])).toBe(false);
    expect(service.hasAnyRole(['Admin', 'Operator'])).toBe(true);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });
});
