import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { AuditEntry } from '../../core/models/audit.model';
import { AuditService } from '../../core/services/audit.service';
import { describeApiError } from '../../core/util/api-error.util';
import { DataStateComponent } from '../../shared/data-state/data-state.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';

@Component({
  selector: 'tr-audit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatPaginatorModule,
    MatTableModule,
    DataStateComponent,
    StatusBadgeComponent
  ],
  templateUrl: './audit.component.html',
  styleUrl: './audit.component.scss'
})
export class AuditComponent implements OnInit {
  private readonly auditService = inject(AuditService);

  protected readonly entries = signal<AuditEntry[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly columns = ['timestampUtc', 'userName', 'entity', 'action', 'changes'];

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.auditService.search(this.pageIndex() + 1, this.pageSize()).subscribe({
      next: page => {
        this.entries.set(page.items);
        this.totalCount.set(page.totalCount);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(describeApiError(error, 'Could not load the audit trail.'));
        this.loading.set(false);
      }
    });
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  protected toneFor(action: string): 'ready' | 'warning' | 'critical' {
    return action === 'Created' ? 'ready' : action === 'Deleted' ? 'critical' : 'warning';
  }

  protected summarise(entry: AuditEntry): string {
    if (entry.action === 'Updated') {
      return entry.changedColumns?.join(', ') ?? '—';
    }

    return entry.action === 'Created' ? 'Record created' : 'Record deleted';
  }
}
