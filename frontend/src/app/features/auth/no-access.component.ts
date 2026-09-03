import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'tr-no-access',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MatButtonModule, MatIconModule],
  template: `
    <div class="no-access">
      <mat-icon aria-hidden="true">lock</mat-icon>
      <h1>This page needs another role</h1>
      <p>
        You are signed in as {{ auth.user()?.email }} with the
        {{ auth.roles().join(', ') || 'no' }} role. An administrator can give you access.
      </p>
      <a matButton="filled" routerLink="/dashboard">Back to the overview</a>
    </div>
  `,
  styles: `
    .no-access {
      max-width: 32rem;
      margin: 4rem auto;
      text-align: center;
      color: var(--tr-muted);
    }

    .no-access mat-icon {
      font-size: 2.5rem;
      width: 2.5rem;
      height: 2.5rem;
      color: var(--tr-warn);
    }

    h1 {
      margin: 0.75rem 0 0.5rem;
      font-size: 1.35rem;
      font-weight: 600;
      color: var(--tr-ink);
    }

    p {
      margin: 0 0 1.5rem;
    }
  `
})
export class NoAccessComponent {
  protected readonly auth = inject(AuthService);
}
