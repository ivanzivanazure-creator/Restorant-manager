import { CurrencyPipe, PercentPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BillingSummary } from '../../core/models/domain.models';
import { KpiCardComponent } from '../../shared/components/kpi-card.component';
import { BillingService } from './billing.service';

@Component({
  selector: 'rsaas-billing',
  standalone: true,
  imports: [CurrencyPipe, PercentPipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule, KpiCardComponent],
  templateUrl: './billing.component.html',
})
export class BillingComponent implements OnInit {
  readonly summary = signal<BillingSummary | null>(null);
  readonly loading = signal(true);
  readonly connecting = signal(false);

  constructor(private readonly billingService: BillingService) {}

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.billingService.getSummary().subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  connect(): void {
    this.connecting.set(true);
    this.billingService.connectStripe().subscribe({
      next: ({ onboardingUrl }) => (window.location.href = onboardingUrl),
      error: () => this.connecting.set(false),
    });
  }
}
