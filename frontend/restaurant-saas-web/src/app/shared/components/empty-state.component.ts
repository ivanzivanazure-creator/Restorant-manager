import { Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'rsaas-empty-state',
  standalone: true,
  imports: [MatIconModule],
  template: `
    <div class="empty-state">
      <mat-icon>{{ icon }}</mat-icon>
      <p>{{ message }}</p>
    </div>
  `,
  styles: [
    `
      .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 3rem 1rem;
        color: var(--rsaas-text-muted);
        text-align: center;
      }
      mat-icon {
        font-size: 2.5rem;
        width: 2.5rem;
        height: 2.5rem;
        margin-bottom: 0.5rem;
        opacity: 0.6;
      }
    `,
  ],
})
export class EmptyStateComponent {
  @Input() message = 'Nothing here yet.';
  @Input() icon = 'inbox';
}
