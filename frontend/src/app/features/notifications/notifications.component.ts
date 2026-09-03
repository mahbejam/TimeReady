import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipListboxChange, MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ReadinessSeverity } from '../../core/models/readiness.model';
import { HrStore } from '../../core/state/hr-store';
import { DataStateComponent } from '../../shared/data-state/data-state.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { VacationDatePipe } from '../../shared/vacation-date.pipe';

type SeverityFilter = 'All' | ReadinessSeverity;

@Component({
  selector: 'tr-notifications',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    DataStateComponent,
    StatusBadgeComponent,
    VacationDatePipe
  ],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss'
})
export class NotificationsComponent implements OnInit {
  protected readonly store = inject(HrStore);
  private readonly snackBar = inject(MatSnackBar);
  protected readonly filters: SeverityFilter[] = ['All', 'Critical', 'Warning', 'Info'];
  protected readonly activeFilter = signal<SeverityFilter>('All');

  /** Employees that have at least one finding, blockers first. */
  protected readonly groups = computed(() => {
    const filter = this.activeFilter();

    return this.store
      .rows()
      .map(row => ({
        row,
        warnings: filter === 'All' ? row.warnings : row.warnings.filter(w => w.severity === filter)
      }))
      .filter(group => group.warnings.length > 0)
      .sort((a, b) => b.row.criticalCount - a.row.criticalCount);
  });

  protected readonly counts = computed(() => {
    const warnings = this.store.rows().flatMap(row => row.warnings);

    return {
      critical: warnings.filter(w => w.severity === 'Critical').length,
      warning: warnings.filter(w => w.severity === 'Warning').length,
      info: warnings.filter(w => w.severity === 'Info').length
    };
  });

  ngOnInit(): void {
    this.store.ensureLoaded();
  }

  protected refresh(): void {
    this.store.refresh$().subscribe({
      next: () =>
        this.snackBar.open('Findings updated.', 'Dismiss', {
          duration: 2500,
          politeness: 'polite'
        }),
      error: () => undefined
    });
  }

  protected onFilterChange(event: MatChipListboxChange): void {
    this.activeFilter.set((event.value as SeverityFilter) ?? 'All');
  }

  protected toneFor(severity: ReadinessSeverity): 'critical' | 'warning' | 'info' {
    return severity === 'Critical' ? 'critical' : severity === 'Warning' ? 'warning' : 'info';
  }
}
