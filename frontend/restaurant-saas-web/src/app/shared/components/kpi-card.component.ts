import { Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'rsaas-kpi-card',
  standalone: true,
  imports: [MatIconModule],
  template: `
    <div class="rsaas-card kpi">
      <div class="kpi-icon" [class.negative]="trendDown">
        <mat-icon>{{ icon }}</mat-icon>
      </div>
      <div class="kpi-body">
        <span class="kpi-label rsaas-muted">{{ label }}</span>
        <span class="kpi-value">{{ value }}</span>
        @if (subtext) {
          <span class="kpi-subtext rsaas-muted">{{ subtext }}</span>
        }
      </div>
    </div>
  `,
  styles: [
    `
      .kpi {
        display: flex;
        align-items: center;
        gap: 1rem;
      }
      .kpi-icon {
        width: 44px;
        height: 44px;
        border-radius: 10px;
        display: flex;
        align-items: center;
        justify-content: center;
        background: color-mix(in srgb, var(--rsaas-primary) 15%, transparent);
        color: var(--rsaas-primary);
        flex-shrink: 0;
      }
      .kpi-icon.negative {
        background: color-mix(in srgb, var(--rsaas-danger) 15%, transparent);
        color: var(--rsaas-danger);
      }
      .kpi-body {
        display: flex;
        flex-direction: column;
        min-width: 0;
      }
      .kpi-label {
        font-size: 0.8rem;
        text-transform: uppercase;
        letter-spacing: 0.03em;
      }
      .kpi-value {
        font-size: 1.6rem;
        font-weight: 700;
        line-height: 1.3;
      }
      .kpi-subtext {
        font-size: 0.8rem;
      }
    `,
  ],
})
export class KpiCardComponent {
  @Input() label = '';
  @Input() value = '';
  @Input() subtext?: string;
  @Input() icon = 'insights';
  @Input() trendDown = false;
}
