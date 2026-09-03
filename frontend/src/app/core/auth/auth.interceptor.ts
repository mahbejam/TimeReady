import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

/** Endpoints that must never carry a token or trigger a refresh loop. */
const AUTH_ENDPOINTS = ['/auth/login', '/auth/refresh'];

/**
 * Attaches the bearer token and, when the API answers 401, refreshes the token
 * once and replays the original request. If the refresh fails as well, the
 * session is dropped and the user is sent to the login page with a return URL.
 *
 * Tokens are only attached to requests aimed at the configured API, so a
 * future third-party call cannot accidentally receive the access token.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!isApiRequest(request.url) || isAuthEndpoint(request.url)) {
    return next(request);
  }

  const token = auth.accessToken;

  return next(token ? withBearer(request, token) : request).pipe(
    catchError((error: unknown) => {
      const isUnauthorized = error instanceof HttpErrorResponse && error.status === 401;

      if (!isUnauthorized || !auth.hasRefreshToken()) {
        return throwError(() => error);
      }

      return auth.refreshTokens().pipe(
        switchMap(refreshedToken => next(withBearer(request, refreshedToken))),
        catchError(() => {
          auth.clearSession();
          void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });

          return throwError(() => error);
        })
      );
    })
  );
};

/**
 * Matches both the configured base URL (absolute in local development, relative
 * behind the nginx proxy) and plain `/api/...` paths used by tests and proxies.
 */
function isApiRequest(url: string): boolean {
  const base = environment.apiBaseUrl;

  if (matchesBase(url, base)) {
    return true;
  }

  const pathPrefix = apiPathPrefix(base);

  return pathPrefix !== base && matchesBase(url, pathPrefix);
}

function matchesBase(url: string, base: string): boolean {
  return url === base || url.startsWith(`${base}/`) || url.startsWith(`${base}?`);
}

function apiPathPrefix(base: string): string {
  if (!base.startsWith('http://') && !base.startsWith('https://')) {
    return base.replace(/\/$/, '');
  }

  try {
    return new URL(base).pathname.replace(/\/$/, '') || '/api';
  } catch {
    return '/api';
  }
}

function isAuthEndpoint(url: string): boolean {
  return AUTH_ENDPOINTS.some(endpoint => url.includes(endpoint));
}

function withBearer<T>(request: HttpRequest<T>, token: string): HttpRequest<T> {
  return request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}
