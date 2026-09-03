import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { Observable, forkJoin, map, switchMap, tap } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { Employee, EmployeeRequest } from '../models/employee.model';
import { ReadinessResult, ReadinessWarning } from '../models/readiness.model';
import { EmployeeService } from '../services/employee.service';
import { ReadinessService } from '../services/readiness.service';
import { describeApiError } from '../util/api-error.util';
import { daysUntil } from '../util/date.util';

/** An employee together with the result of the readiness check. */
export interface EmployeeRow extends Employee {
  isReady: boolean;
  status: string;
  warnings: ReadinessWarning[];
  criticalCount: number;
  daysUntilVacation: number | null;
}

export interface HrSummary {
  total: number;
  ready: number;
  notReady: number;
  openCritical: number;
  readinessScore: number;
}

/**
 * Single source of truth for the three pages. Employees and their readiness
 * results always arrive together, so one store keeps them consistent and saves
 * every page from repeating the same two requests.
 */
@Injectable({ providedIn: 'root' })
export class HrStore {
  private readonly employeeService = inject(EmployeeService);
  private readonly readinessService = inject(ReadinessService);
  private readonly auth = inject(AuthService);

  private readonly employeesState = signal<Employee[]>([]);
  private readonly readinessState = signal<ReadinessResult[]>([]);
  private readonly loadingState = signal(false);
  private readonly errorState = signal<string | null>(null);
  private readonly loadedState = signal(false);

  /** Bumps on every load so a slower response cannot overwrite a newer one. */
  private loadGeneration = 0;

  constructor() {
    // The store outlives a session, so it has to be emptied when one ends.
    // Without this, signing in as a different user would briefly show the
    // previous user's data, and `ensureLoaded` would never fetch it again.
    effect(() => {
      if (!this.auth.isAuthenticated()) {
        this.reset();
      }
    });
  }

  readonly loading = this.loadingState.asReadonly();
  readonly error = this.errorState.asReadonly();
  readonly loaded = this.loadedState.asReadonly();

  readonly rows = computed<EmployeeRow[]>(() => {
    const readinessByEmployee = new Map(
      this.readinessState().map(result => [result.employeeId, result])
    );

    return this.employeesState().map(employee => {
      const readiness = readinessByEmployee.get(employee.id);
      const warnings = readiness?.warnings ?? [];

      return {
        ...employee,
        isReady: readiness?.isReady ?? false,
        status: readiness?.status ?? 'Not Ready',
        warnings,
        criticalCount: warnings.filter(warning => warning.severity === 'Critical').length,
        daysUntilVacation: daysUntil(employee.vacationStartDate)
      };
    });
  });

  readonly summary = computed<HrSummary>(() => {
    const rows = this.rows();
    const ready = rows.filter(row => row.isReady).length;

    return {
      total: rows.length,
      ready,
      notReady: rows.length - ready,
      openCritical: rows.reduce((sum, row) => sum + row.criticalCount, 0),
      readinessScore: rows.length === 0 ? 0 : Math.round((ready / rows.length) * 100)
    };
  });

  /** Employees with something blocking, most urgent departure first. */
  readonly needsAttention = computed(() =>
    this.rows()
      .filter(row => row.criticalCount > 0)
      .sort((a, b) => (a.daysUntilVacation ?? 9999) - (b.daysUntilVacation ?? 9999))
  );

  readonly upcomingDepartures = computed(() =>
    this.rows()
      .filter(row => row.daysUntilVacation !== null && row.daysUntilVacation >= 0)
      .sort((a, b) => (a.daysUntilVacation ?? 0) - (b.daysUntilVacation ?? 0))
      .slice(0, 5)
  );

  load(): void {
    this.reload$().subscribe({ error: () => undefined });
  }

  /** Same as load, but returns the observable so callers can show success feedback. */
  refresh$(): Observable<void> {
    return this.reload$();
  }

  /** Drops everything that belonged to the previous session. */
  reset(): void {
    this.loadGeneration += 1;
    this.employeesState.set([]);
    this.readinessState.set([]);
    this.errorState.set(null);
    this.loadingState.set(false);
    this.loadedState.set(false);
  }

  /** Loads once per session; pages call this instead of `load` on every visit. */
  ensureLoaded(): void {
    if (!this.loadedState() && !this.loadingState()) {
      this.load();
    }
  }

  create(request: EmployeeRequest): Observable<Employee> {
    return this.employeeService.create(request).pipe(
      switchMap(created => this.reload$().pipe(map(() => created)))
    );
  }

  update(id: number, request: EmployeeRequest): Observable<void> {
    return this.employeeService.update(id, request).pipe(
      switchMap(() => this.reload$())
    );
  }

  remove(id: number): Observable<void> {
    return this.employeeService.remove(id).pipe(
      switchMap(() => this.reload$())
    );
  }

  private reload$(): Observable<void> {
    const generation = ++this.loadGeneration;

    this.loadingState.set(true);
    this.errorState.set(null);

    return forkJoin({
      employees: this.employeeService.list(),
      readiness: this.readinessService.list()
    }).pipe(
      tap({
        next: ({ employees, readiness }) => {
          if (generation !== this.loadGeneration) {
            return;
          }

          this.employeesState.set(employees);
          this.readinessState.set(readiness);
          this.loadingState.set(false);
          this.loadedState.set(true);
        },
        error: (error: unknown) => {
          if (generation !== this.loadGeneration) {
            return;
          }

          this.errorState.set(describeApiError(error, 'Could not load the team overview.'));
          this.loadingState.set(false);
        }
      }),
      map(() => undefined)
    );
  }
}
