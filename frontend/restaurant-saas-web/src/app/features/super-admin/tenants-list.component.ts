import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { forkJoin } from 'rxjs';
import { PlatformAnalytics, TenantSummary } from '../../core/models/domain.models';
import { KpiCardComponent } from '../../shared/components/kpi-card.component';
import { SuperAdminService } from './super-admin.service';

@Component({
  selector: 'rsaas-tenants-list',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, MatButtonModule, MatIconModule, MatChipsModule, MatProgressSpinnerModule, KpiCardComponent],
  templateUrl: './tenants-list.component.html',
})
export class TenantsListComponent implements OnInit {
  readonly tenants = signal<TenantSummary[]>([]);
  readonly analytics = signal<PlatformAnalytics | null>(null);
  readonly loading = signal(true);

  constructor(
    private readonly superAdminService: SuperAdminService,
    private readonly snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    forkJoin({
      tenants: this.superAdminService.getTenants(),
      analytics: this.superAdminService.getAnalytics(),
    }).subscribe({
      next: ({ tenants, analytics }) => {
        this.tenants.set(tenants.items);
        this.analytics.set(analytics);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  deactivate(tenant: TenantSummary): void {
    this.superAdminService.deactivateTenant(tenant.id).subscribe(() => {
      this.snackBar.open(`${tenant.companyName} deactivated`, 'Dismiss', { duration: 3000 });
      this.reload();
    });
  }

  statusColor(status: TenantSummary['subscriptionStatus']): string {
    switch (status) {
      case 'Active':
        return 'primary';
      case 'Locked':
      case 'PastDue':
        return 'warn';
      default:
        return '';
    }
  }
}
