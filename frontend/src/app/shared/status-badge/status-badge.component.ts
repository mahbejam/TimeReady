import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type BadgeTone = 'ready' | 'not-ready' | 'info' | 'warning' | 'critical';

/**
 * Small, flat status label. Colour carries the meaning, the text repeats it so
 * the badge also works without colour perception.
 */
@Component({
  selector: 'tr-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="badge" [class]="'badge--' + tone()">{{ label() }}</span>`,
  styles: `
    .badge {
      display: inline-block;
      padding: 0.15rem 0.6rem;
      border-radius: 999px;
      font-size: 0.78rem;
      font-weight: 600;
      line-height: 1.5;
      white-space: nowrap;
      border: 1px solid transparent;
    }

    .badge--ready {
      color: var(--tr-ok);
      background: rgba(27, 127, 90, 0.1);
      border-color: rgba(27, 127, 90, 0.25);
    }

    .badge--not-ready,
    .badge--critical {
      color: var(--tr-critical);
      background: rgba(179, 38, 30, 0.09);
      border-color: rgba(179, 38, 30, 0.25);
    }

    .badge--warning {
      color: var(--tr-warn);
      background: rgba(178, 106, 0, 0.1);
      border-color: rgba(178, 106, 0, 0.25);
    }

    .badge--info {
      color: #4a5560;
      background: rgba(74, 85, 96, 0.08);
      border-color: rgba(74, 85, 96, 0.2);
    }
  `
})
export class StatusBadgeComponent {
  readonly label = input.required<string>();
  readonly tone = input.required<BadgeTone>();
}
