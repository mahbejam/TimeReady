import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

/**
 * Team readiness at a glance. The ring is a plain conic gradient – no canvas,
 * no chart library, no animation.
 */
@Component({
  selector: 'tr-readiness-score-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatCardModule],
  template: `
    <mat-card class="score" appearance="outlined">
      <mat-card-content class="score__content">
        <div class="score__ring" [style.background]="ringBackground()" role="img"
             [attr.aria-label]="score() + ' percent of the team is ready'">
          <div class="score__inner">
            <span class="score__value">{{ score() }}<span class="score__unit">%</span></span>
            <span class="score__caption">ready</span>
          </div>
        </div>

        <dl class="score__facts">
          <div>
            <dt>Ready to leave</dt>
            <dd>{{ ready() }} of {{ total() }}</dd>
          </div>
          <div>
            <dt>Open blockers</dt>
            <dd [class.score__critical]="openCritical() > 0">{{ openCritical() }}</dd>
          </div>
          <div>
            <dt>Still to prepare</dt>
            <dd>{{ notReady() }}</dd>
          </div>
        </dl>
      </mat-card-content>
    </mat-card>
  `,
  styles: `
    .score__content {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 1.75rem;
      padding: 0.5rem 0;
    }

    .score__ring {
      width: 128px;
      height: 128px;
      border-radius: 50%;
      display: grid;
      place-items: center;
      flex: 0 0 auto;
    }

    .score__inner {
      width: 100px;
      height: 100px;
      border-radius: 50%;
      background: #fff;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      line-height: 1;
    }

    .score__value {
      font-size: 1.9rem;
      font-weight: 600;
      font-variant-numeric: tabular-nums;
      color: #1c1f24;
    }

    .score__unit {
      font-size: 1rem;
      font-weight: 500;
      margin-left: 0.1rem;
    }

    .score__caption {
      margin-top: 0.35rem;
      font-size: 0.78rem;
      letter-spacing: 0.06em;
      text-transform: uppercase;
      color: #5b6470;
    }

    .score__facts {
      display: grid;
      gap: 0.9rem;
      margin: 0;
      flex: 1 1 12rem;
    }

    .score__facts dt {
      font-size: 0.8rem;
      color: #5b6470;
    }

    .score__facts dd {
      margin: 0.1rem 0 0;
      font-size: 1.15rem;
      font-weight: 600;
      font-variant-numeric: tabular-nums;
      color: #1c1f24;
    }

    .score__critical {
      color: var(--tr-critical);
    }
  `
})
export class ReadinessScoreCardComponent {
  readonly score = input.required<number>();
  readonly ready = input.required<number>();
  readonly notReady = input.required<number>();
  readonly total = input.required<number>();
  readonly openCritical = input.required<number>();

  protected readonly ringBackground = computed(() => {
    const filled = `${this.score() * 3.6}deg`;
    const colour = this.score() >= 80 ? 'var(--tr-ok)' : this.score() >= 50 ? 'var(--tr-warn)' : 'var(--tr-critical)';

    return `conic-gradient(${colour} 0deg ${filled}, var(--tr-border) ${filled} 360deg)`;
  });
}
