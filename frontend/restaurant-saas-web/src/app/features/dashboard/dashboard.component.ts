import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LocationContextService } from '../../core/services/location-context.service';
import { KpiCardComponent } from '../../shared/components/kpi-card.component';
import { DashboardSummary } from '../../core/models/domain.models';
import { DashboardService } from './dashboard.service';
import { OnboardingChecklistComponent } from './onboarding-checklist.component';

@Component({
  selector: 'rsaas-dashboard',
  standalone: true,
  imports: [CurrencyPipe, MatIconModule, MatChipsModule, MatProgressSpinnerModule, KpiCardComponent, OnboardingChecklistComponent],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  readonly summary = signal<DashboardSummary | null>(null);
  readonly loading = signal(true);

  constructor(
    private readonly dashboardService: DashboardService,
    readonly locationContext: LocationContextService,
  ) {}

  ngOnInit(): void {
    const locationId = this.locationContext.locationId();
    if (!locationId) {
      this.loading.set(false);
      return;
    }

    this.dashboardService.getSummary(locationId).subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
