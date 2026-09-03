import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, map, of, shareReplay, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, AuthUser, LoginRequest, Role } from '../models/auth.model';
import { TokenStorage } from './token-storage';

/**
 * Owns the signed-in session: the access token, the current user and the
 * refresh handshake. Everything else in the app reads the signals it exposes.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storage = inject(TokenStorage);
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  /** Kept in memory on purpose – see TokenStorage for the reasoning. */
  private accessTokenValue: string | null = null;

  /** Shared between callers so a burst of 401s triggers one refresh, not five. */
  private refreshInFlight: Observable<string> | null = null;

  private readonly userState = signal<AuthUser | null>(null);

  readonly user = this.userState.asReadonly();
  readonly isAuthenticated = computed(() => this.userState() !== null);
  readonly roles = computed<Role[]>(() => this.userState()?.roles ?? []);
  readonly isAdmin = computed(() => this.roles().includes('Admin'));

  get accessToken(): string | null {
    return this.accessTokenValue;
  }

  hasRefreshToken(): boolean {
    return this.storage.refreshToken !== null;
  }

  hasAnyRole(roles: Role[]): boolean {
    return roles.some(role => this.roles().includes(role));
  }

  login(request: LoginRequest): Observable<AuthUser> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, request).pipe(
      tap(response => this.applySession(response)),
      map(response => response.user)
    );
  }

  /**
   * Exchanges the stored refresh token for a new pair. Concurrent callers share
   * one request; a failure clears the session, because the refresh token is the
   * only thing that could have restored it.
   */
  refreshTokens(): Observable<string> {
    if (this.refreshInFlight) {
      return this.refreshInFlight;
    }

    const refreshToken = this.storage.refreshToken;

    if (!refreshToken) {
      return throwError(() => new Error('No refresh token is stored.'));
    }

    this.refreshInFlight = this.http
      .post<AuthResponse>(`${this.baseUrl}/refresh`, { refreshToken })
      .pipe(
        tap(response => this.applySession(response)),
        map(response => response.accessToken),
        catchError((error: unknown) => {
          this.clearSession();

          return throwError(() => error);
        }),
        finalize(() => (this.refreshInFlight = null)),
        shareReplay({ bufferSize: 1, refCount: false })
      );

    return this.refreshInFlight;
  }

  /** Revokes the refresh token server side, then clears the local session. */
  logout(): Observable<void> {
    const refreshToken = this.storage.refreshToken;

    if (!refreshToken || !this.accessTokenValue) {
      this.clearSession();

      return of(void 0);
    }

    return this.http.post<void>(`${this.baseUrl}/logout`, { refreshToken }).pipe(
      // A failed revoke must not trap the user in a session they wanted to end.
      catchError(() => of(void 0)),
      tap(() => this.clearSession())
    );
  }

  /** Drops the local session without calling the API. Used when a refresh fails. */
  clearSession(): void {
    this.accessTokenValue = null;
    this.userState.set(null);
    this.storage.clear();
  }

  /**
   * Called once at startup: if a refresh token survived a page reload, trade it
   * for a fresh access token so the user stays signed in.
   */
  restoreSession(): Promise<void> {
    if (!this.hasRefreshToken()) {
      return Promise.resolve();
    }

    return new Promise(resolve => {
      this.refreshTokens().subscribe({
        next: () => resolve(),
        error: () => resolve()
      });
    });
  }

  private applySession(response: AuthResponse): void {
    this.accessTokenValue = response.accessToken;
    this.storage.refreshToken = response.refreshToken;
    this.userState.set(response.user);
  }
}
