import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

/** Only the members the interceptor actually uses. */
class AuthServiceStub {
  accessToken: string | null = 'access-1';
  refreshToken: string | null = 'refresh-1';
  refreshResult: Observable<string> = of('access-2');
  clearSessionCalls = 0;

  hasRefreshToken(): boolean {
    return this.refreshToken !== null;
  }

  refreshTokens(): Observable<string> {
    return this.refreshResult;
  }

  clearSession(): void {
    this.clearSessionCalls += 1;
    this.accessToken = null;
    this.refreshToken = null;
  }
}

describe('authInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let auth: AuthServiceStub;
  let navigate: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    auth = new AuthServiceStub();
    navigate = vi.fn().mockResolvedValue(true);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: { navigate, url: '/employees' } }
      ]
    });

    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  it('attaches the bearer token to an API request', () => {
    http.get('/api/employees').subscribe();

    const request = controller.expectOne('/api/employees');

    expect(request.request.headers.get('Authorization')).toBe('Bearer access-1');
    request.flush([]);
    controller.verify();
  });

  it('sends no token when nobody is signed in', () => {
    auth.accessToken = null;

    http.get('/api/employees').subscribe();

    const request = controller.expectOne('/api/employees');

    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush([]);
    controller.verify();
  });

  it('leaves the login and refresh calls untouched', () => {
    http.post('/api/auth/login', {}).subscribe();
    http.post('/api/auth/refresh', {}).subscribe();

    for (const url of ['/api/auth/login', '/api/auth/refresh']) {
      const request = controller.expectOne(url);

      expect(request.request.headers.has('Authorization')).toBe(false);
      request.flush({});
    }

    controller.verify();
  });

  it('refreshes once after a 401 and replays the original request', () => {
    let body: unknown = null;

    http.get('/api/employees').subscribe(result => (body = result));

    controller
      .expectOne('/api/employees')
      .flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    const replay = controller.expectOne('/api/employees');

    expect(replay.request.headers.get('Authorization')).toBe('Bearer access-2');
    replay.flush([{ id: 1 }]);

    expect(body).toEqual([{ id: 1 }]);
    controller.verify();
  });

  it('signs the user out and returns to the login page when the refresh fails', () => {
    auth.refreshResult = throwError(() => new Error('refresh rejected'));

    let status = 0;

    http.get('/api/employees').subscribe({ error: error => (status = error.status) });

    controller
      .expectOne('/api/employees')
      .flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(status).toBe(401);
    expect(auth.clearSessionCalls).toBe(1);
    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: '/employees' } });
    controller.verify();
  });

  it('does not try to refresh when there is no refresh token', () => {
    auth.refreshToken = null;

    let status = 0;

    http.get('/api/employees').subscribe({ error: error => (status = error.status) });

    controller
      .expectOne('/api/employees')
      .flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(status).toBe(401);
    expect(navigate).not.toHaveBeenCalled();
    controller.verify();
  });

  it('passes other errors through unchanged', () => {
    let status = 0;

    http.get('/api/employees').subscribe({ error: error => (status = error.status) });

    controller
      .expectOne('/api/employees')
      .flush({ title: 'Forbidden' }, { status: 403, statusText: 'Forbidden' });

    expect(status).toBe(403);
    controller.verify();
  });

  it('does not attach a token to a non-API request', () => {
    http.get('https://cdn.example.com/asset.json').subscribe();

    const request = controller.expectOne('https://cdn.example.com/asset.json');

    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
    controller.verify();
  });
});
