import { DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ComponentHealth, PublicStatus } from '../../core/models/domain.models';
import { StatusService } from './status.service';

const COMPONENT_LABELS: Record<string, string> = {
  Api: 'API',
  Database: 'Database',
  Cache: 'Cache',
  Realtime: 'Real-time (Kitchen/Orders)',
  BackgroundJobs: 'Background Jobs',
};

const HEALTH_LABELS: Record<ComponentHealth, string> = {
  Operational: 'Operational',
  DegradedPerformance: 'Degraded performance',
  PartialOutage: 'Partial outage',
  MajorOutage: 'Major outage',
};

@Component({
  selector: 'rsaas-status-page',
  standalone: true,
  imports: [DatePipe, MatIconModule, MatChipsModule, MatProgressSpinnerModule],
  templateUrl: './status-page.component.html',
  styleUrl: './status-page.component.scss',
})
export class StatusPageComponent implements OnInit {
  readonly status = signal<PublicStatus | null>(null);
  readonly loading = signal(true);
  readonly componentLabels = COMPONENT_LABELS;
  readonly healthLabels = HEALTH_LABELS;

  constructor(private readonly statusService: StatusService) {}

  ngOnInit(): void {
    this.statusService.getStatus().subscribe({
      next: (status) => {
        this.status.set(status);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  healthClass(health: ComponentHealth): string {
    return health.toLowerCase();
  }
}
