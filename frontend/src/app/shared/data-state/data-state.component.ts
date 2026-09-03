import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

/**
 * Wraps a page section and shows the loading, error and empty states in one
 * place, so every page reports problems the same way.
 */
@Component({
  selector: 'tr-data-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    @if (loading()) {
      <div class="state" role="status" aria-live="polite">
        <mat-spinner diameter="36" />
        <p class="state__text">{{ loadingText() }}</p>
      </div>
    } @else if (error()) {
      <div class="state state--error" role="alert">
        <mat-icon aria-hidden="true">error_outline</mat-icon>
        <p class="state__text">{{ error() }}</p>
        <button matButton="outlined" type="button" (click)="retry.emit()">Try again</button>
      </div>
    } @else if (empty()) {
      <div class="state">
        <p class="state__text">{{ emptyText() }}</p>
        <ng-content select="[emptyAction]" />
      </div>
    } @else {
      <ng-content />
    }
  `,
  styles: `
    .state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.75rem;
      padding: 3rem 1rem;
      text-align: center;
      color: #5b6470;
    }

    .state--error mat-icon {
      color: var(--tr-critical);
    }

    .state__text {
      margin: 0;
      max-width: 34rem;
    }
  `
})
export class DataStateComponent {
  readonly loading = input(false);
  readonly error = input<string | null>(null);
  readonly empty = input(false);
  readonly loadingText = input('Loading…');
  readonly emptyText = input('Nothing to show yet.');

  readonly retry = output<void>();
}
