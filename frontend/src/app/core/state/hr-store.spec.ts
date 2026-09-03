import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { AuthResponse } from '../models/auth.model';
import { AuthService } from '../auth/auth.service';
import { HrStore } from './hr-store';

const session: AuthResponse = {
  accessToken: 'access-1',
  expiresAtUtc: '2026-07-24T15:00:00+00:00',
  refreshToken: 'refresh-1',
  refreshTokenExpiresAtUtc: '2026-07-31T14:30:00+00:00',
  user: {
    id: 'user-1',
    email: 'admin@timeready.local',
    fullName: 'TimeReady Administrator',
    roles: ['Admin']
  }
};

describe('HrStore', () => {
  let store: HrStore;
  let auth: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    auth = TestBed.inject(AuthService);
    store = TestBed.inject(HrStore);
    http = TestBed.inject(HttpTestingController);

    auth.login({ email: 'admin@timeready.local', password: 'x' }).subscribe();
    http.expectOne(request => request.url.endsWith('/auth/login')).flush(session);

    TestBed.tick();
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  function loadTeam(): void {
    store.load();

    http.expectOne(request => request.url.endsWith('/employees')).flush([
      {
        id: 1,
        fullName: 'Anna Gruber',
        timeBalanceHours: 12.5,
        remainingVacationDays: 18,
        vacationStartDate: null,
        managerInformed: true,
        handoverCompleted: false
      }
    ]);

    http.expectOne(request => request.url.endsWith('/readiness')).flush([
      { employeeId: 1, fullName: 'Anna Gruber', isReady: false, status: 'Not Ready', warnings: [] }
    ]);
  }

  it('merges employees with their readiness result', () => {
    loadTeam();

    const row = store.rows()[0];

    expect(row.fullName).toBe('Anna Gruber');
    expect(row.status).toBe('Not Ready');
    expect(store.summary().total).toBe(1);
    expect(store.loaded()).toBe(true);
  });

  it('empties itself when the session ends, so the next user starts clean', () => {
    loadTeam();

    expect(store.rows()).toHaveLength(1);

    auth.clearSession();
    TestBed.tick();

    expect(store.rows()).toHaveLength(0);
    expect(store.summary().total).toBe(0);
    // `loaded` has to go back to false, otherwise `ensureLoaded` would never
    // fetch anything for the next user.
    expect(store.loaded()).toBe(false);
  });

  it('fetches again for the next session', () => {
    loadTeam();
    auth.clearSession();
    TestBed.tick();

    store.ensureLoaded();

    http.expectOne(request => request.url.endsWith('/employees')).flush([]);
    http.expectOne(request => request.url.endsWith('/readiness')).flush([]);

    expect(store.loaded()).toBe(true);
  });
});
