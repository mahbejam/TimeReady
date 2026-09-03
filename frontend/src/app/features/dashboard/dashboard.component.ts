import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HrStore } from '../../core/state/hr-store';
import { DataStateComponent } from '../../shared/data-state/data-state.component';
import { ReadinessScoreCardComponent } from '../../shared/readiness-score-card/readiness-score-card.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { VacationDatePipe } from '../../shared/vacation-date.pipe';

@Component({
  selector: 'tr-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    DataStateComponent,
    ReadinessScoreCardComponent,
    StatusBadgeComponent,
    VacationDatePipe
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  protected readonly store = inject(HrStore);
  private readonly snackBar = inject(MatSnackBar);

  ngOnInit(): void {
    this.store.ensureLoaded();
  }

  protected refresh(): void {
    this.store.refresh$().subscribe({
      next: () =>
        this.snackBar.open('Overview updated.', 'Dismiss', {
          duration: 2500,
          politeness: 'polite'
        }),
      error: () => undefined
    });
  }
}
