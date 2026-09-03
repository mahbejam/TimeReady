import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/auth/auth.service';
import { EmployeeRow, HrStore } from '../../core/state/hr-store';
import { describeApiError } from '../../core/util/api-error.util';
import { DataStateComponent } from '../../shared/data-state/data-state.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { EmployeeDialogComponent, EmployeeDialogData } from './employee-dialog/employee-dialog.component';
import { VacationDatePipe } from '../../shared/vacation-date.pipe';

@Component({
  selector: 'tr-employee-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DecimalPipe,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatTableModule,
    MatTooltipModule,
    DataStateComponent,
    StatusBadgeComponent,
    VacationDatePipe
  ],
  templateUrl: './employee-list.component.html',
  styleUrl: './employee-list.component.scss'
})
export class EmployeeListComponent implements OnInit {
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  protected readonly store = inject(HrStore);
  protected readonly auth = inject(AuthService);

  protected readonly filter = signal('');

  protected readonly columns = [
    'fullName',
    'timeBalanceHours',
    'remainingVacationDays',
    'vacationStartDate',
    'preparation',
    'status',
    'actions'
  ];

  /** Creating and deleting are Admin-only on the API, so the UI matches. */
  protected readonly canManage = computed(() => this.auth.isAdmin());

  protected readonly filteredRows = computed(() => {
    const term = this.filter().trim().toLowerCase();
    const rows = this.store.rows();

    return term ? rows.filter(row => row.fullName.toLowerCase().includes(term)) : rows;
  });

  ngOnInit(): void {
    this.store.ensureLoaded();
  }

  protected openCreateDialog(): void {
    this.openDialog(null);
  }

  protected openEditDialog(row: EmployeeRow): void {
    this.openDialog(row);
  }

  protected delete(row: EmployeeRow): void {
    const confirmed = confirm(`Delete ${row.fullName}? This cannot be undone.`);

    if (!confirmed) {
      return;
    }

    this.store.remove(row.id).subscribe({
      next: () => this.snackBar.open(`${row.fullName} deleted`, 'Dismiss', { duration: 4000 }),
      error: (error: unknown) =>
        this.snackBar.open(describeApiError(error, 'The employee could not be deleted.'), 'Dismiss', {
          duration: 6000
        })
    });
  }

  protected onFilterInput(value: string): void {
    this.filter.set(value);
  }

  private openDialog(employee: EmployeeRow | null): void {
    const data: EmployeeDialogData = {
      employee: employee
        ? {
            id: employee.id,
            fullName: employee.fullName,
            timeBalanceHours: employee.timeBalanceHours,
            remainingVacationDays: employee.remainingVacationDays,
            vacationStartDate: employee.vacationStartDate,
            managerInformed: employee.managerInformed,
            handoverCompleted: employee.handoverCompleted
          }
        : null
    };

    this.dialog
      .open<EmployeeDialogComponent, EmployeeDialogData, boolean>(EmployeeDialogComponent, {
        data,
        width: '620px',
        maxWidth: '95vw',
        autoFocus: 'first-tabbable'
      })
      .afterClosed()
      .subscribe(saved => {
        if (saved) {
          this.snackBar.open(employee ? 'Changes saved' : 'Employee added', 'Dismiss', { duration: 4000 });
        }
      });
  }
}
