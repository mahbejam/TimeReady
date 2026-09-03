import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { EMPTY, Observable, catchError, debounceTime, startWith, switchMap } from 'rxjs';
import { Employee, EmployeeRequest } from '../../../core/models/employee.model';
import { ReadinessResult } from '../../../core/models/readiness.model';
import { ReadinessService } from '../../../core/services/readiness.service';
import { HrStore } from '../../../core/state/hr-store';
import { describeApiError, extractValidationErrors } from '../../../core/util/api-error.util';
import { fromIsoDate, toIsoDate } from '../../../core/util/date.util';
import { StatusBadgeComponent } from '../../../shared/status-badge/status-badge.component';

export interface EmployeeDialogData {
  employee: Employee | null;
}

@Component({
  selector: 'tr-employee-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    StatusBadgeComponent
  ],
  templateUrl: './employee-dialog.component.html',
  styleUrl: './employee-dialog.component.scss'
})
export class EmployeeDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly store = inject(HrStore);
  private readonly readinessService = inject(ReadinessService);
  private readonly dialogRef = inject(MatDialogRef<EmployeeDialogComponent, boolean>);
  private readonly data = inject<EmployeeDialogData>(MAT_DIALOG_DATA);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly employee = this.data.employee;
  protected readonly isEdit = this.employee !== null;
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly preview = signal<ReadinessResult | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    fullName: [this.employee?.fullName ?? '', [Validators.required, Validators.maxLength(120)]],
    timeBalanceHours: [
      this.employee?.timeBalanceHours ?? 0,
      [Validators.required, Validators.min(-200), Validators.max(400)]
    ],
    remainingVacationDays: [
      this.employee?.remainingVacationDays ?? 0,
      [Validators.required, Validators.min(0), Validators.max(60)]
    ],
    vacationStartDate: [fromIsoDate(this.employee?.vacationStartDate ?? null)],
    managerInformed: [this.employee?.managerInformed ?? false],
    handoverCompleted: [this.employee?.handoverCompleted ?? false]
  });

  constructor() {
    // Live preview of the rule engine while the form is being filled in.
    // switchMap cancels an in-flight preview when the form changes again.
    this.form.valueChanges
      .pipe(
        startWith(null),
        debounceTime(400),
        takeUntilDestroyed(this.destroyRef),
        switchMap(() => this.previewRequest())
      )
      .subscribe(result => this.preview.set(result));
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.errorMessage.set(null);

    const request = this.toRequest();
    // Update returns void, create returns the new employee – the dialog only
    // cares that the call succeeded.
    const save$: Observable<unknown> = this.employee
      ? this.store.update(this.employee.id, request)
      : this.store.create(request);

    save$.subscribe({
      next: () => this.dialogRef.close(true),
      error: (error: unknown) => {
        this.saving.set(false);
        this.applyServerErrors(error);
      }
    });
  }

  protected cancel(): void {
    this.dialogRef.close(false);
  }

  private previewRequest() {
    if (this.form.invalid) {
      this.preview.set(null);
      return EMPTY;
    }

    return this.readinessService.evaluate(this.toRequest()).pipe(
      catchError(() => {
        this.preview.set(null);
        return EMPTY;
      })
    );
  }

  private toRequest(): EmployeeRequest {
    const value = this.form.getRawValue();

    return {
      fullName: value.fullName.trim(),
      timeBalanceHours: Number(value.timeBalanceHours),
      remainingVacationDays: Number(value.remainingVacationDays),
      vacationStartDate: toIsoDate(value.vacationStartDate),
      managerInformed: value.managerInformed,
      handoverCompleted: value.handoverCompleted
    };
  }

  /** Mirrors server-side validation back onto the matching form fields. */
  private applyServerErrors(error: unknown): void {
    const validationErrors = extractValidationErrors(error);

    if (!validationErrors) {
      this.errorMessage.set(describeApiError(error, 'The employee could not be saved.'));
      return;
    }

    let unmatched: string | null = null;

    for (const [property, messages] of Object.entries(validationErrors)) {
      const controlName = property.charAt(0).toLowerCase() + property.slice(1);
      const control = this.form.get(controlName);

      if (control) {
        control.setErrors({ server: messages[0] });
        control.markAsTouched();
      } else {
        unmatched = messages[0];
      }
    }

    this.errorMessage.set(unmatched);
  }
}
