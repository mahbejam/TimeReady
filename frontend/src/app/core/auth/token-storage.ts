import { Injectable } from '@angular/core';

/**
 * Keeps the refresh token between page reloads.
 *
 * A browser application has no genuinely secure place for a token: anything
 * JavaScript can read, injected JavaScript can read too. This is the compromise
 * the project makes, and it is deliberate:
 *
 * - The **access token lives in memory only** and is never written to storage,
 *   so it disappears the moment the tab is closed.
 * - The **refresh token goes to `sessionStorage`**, not `localStorage`: it is
 *   scoped to the tab and cleared when the tab closes, which keeps a shared or
 *   forgotten browser from holding a usable session.
 *
 * The stronger option is for the API to return the refresh token as an
 * `HttpOnly; Secure; SameSite=Strict` cookie, which script cannot read at all.
 * That needs a backend change and is the natural next step.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorage {
  private static readonly RefreshTokenKey = 'timeready.refresh-token';

  get refreshToken(): string | null {
    try {
      return sessionStorage.getItem(TokenStorage.RefreshTokenKey);
    } catch {
      // Storage can be unavailable in private modes; the session then simply
      // does not survive a reload.
      return null;
    }
  }

  set refreshToken(value: string | null) {
    try {
      if (value === null) {
        sessionStorage.removeItem(TokenStorage.RefreshTokenKey);
      } else {
        sessionStorage.setItem(TokenStorage.RefreshTokenKey, value);
      }
    } catch {
      // Ignore: the application still works for the current page view.
    }
  }

  clear(): void {
    this.refreshToken = null;
  }
}
