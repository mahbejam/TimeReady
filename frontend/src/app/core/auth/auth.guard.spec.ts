import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { beforeEach, describe, expect, it } from 'vitest';
import { Role } from '../models/auth.model';
import { AuthService } from './auth.service';
import { authGuard, guestGuard, roleGuard } from './auth.guard';

class AuthServiceStub {
  private readonly rolesState = signal<Role[]>([]);
  private readonly signedIn = signal(false);

  readonly roles = this.rolesState.asReadonly();

  isAuthenticated = () => this.signedIn();

  isAdmin = () => this.rolesState().includes('Admin');

  hasAnyRole = (roles: Role[]) => roles.some(role => this.rolesState().includes(role));

  signIn(roles: Role[]): void {
    this.rolesState.set(roles);
    this.signedIn.set(true);
  }
}

describe('auth guards', () => {
  let auth: AuthServiceStub;
  let router: Router;

  const state = { url: '/employees' } as RouterStateSnapshot;
  const route = {} as ActivatedRouteSnapshot;

  beforeEach(() => {
    auth = new AuthServiceStub();

    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }]
    });

    router = TestBed.inject(Router);
  });

  function run<T>(guard: () => T): T {
    return TestBed.runInInjectionContext(guard);
  }

  it('authGuard lets a signed-in user through', () => {
    auth.signIn(['Operator']);

    expect(run(() => authGuard(route, state))).toBe(true);
  });

  it('authGuard redirects to the login page and remembers where the user wanted to go', () => {
    const result = run(() => authGuard(route, state)) as UrlTree;

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result)).toBe('/login?returnUrl=%2Femployees');
  });

  it('roleGuard lets a matching role through', () => {
    auth.signIn(['Admin']);

    expect(run(() => roleGuard('Admin')(route, state))).toBe(true);
  });

  it('roleGuard sends the wrong role to the no-access page', () => {
    auth.signIn(['Operator']);

    const result = run(() => roleGuard('Admin')(route, state)) as UrlTree;

    expect(router.serializeUrl(result)).toBe('/no-access');
  });

  it('roleGuard sends a signed-out visitor to the login page, not to no-access', () => {
    const result = run(() => roleGuard('Admin')(route, state)) as UrlTree;

    expect(router.serializeUrl(result)).toBe('/login?returnUrl=%2Femployees');
  });

  it('roleGuard accepts any of the listed roles', () => {
    auth.signIn(['Operator']);

    expect(run(() => roleGuard('Admin', 'Operator')(route, state))).toBe(true);
  });

  it('guestGuard keeps a signed-in user away from the login page', () => {
    auth.signIn(['Admin']);

    const result = run(() => guestGuard(route, state)) as UrlTree;

    expect(router.serializeUrl(result)).toBe('/dashboard');
  });

  it('guestGuard lets a visitor see the login page', () => {
    expect(run(() => guestGuard(route, state))).toBe(true);
  });
});
