import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Incident, IncidentSeverity, IncidentStatus, PlatformComponentName } from '../../core/models/domain.models';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { IncidentsService } from './incidents.service';

@Component({
  selector: 'rsaas-incidents',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, MatButtonModule, MatIconModule, MatChipsModule, MatFormFieldModule, MatInputModule, MatSelectModule, EmptyStateComponent],
  templateUrl: './incidents.component.html',
})
export class IncidentsComponent implements OnInit {
  readonly severities: IncidentSeverity[] = ['Minor', 'Major', 'Critical'];
  readonly components: PlatformComponentName[] = ['Api', 'Database', 'Cache', 'Realtime', 'BackgroundJobs'];
  readonly statuses: IncidentStatus[] = ['Investigating', 'Identified', 'Monitoring', 'Resolved'];

  private readonly fb = inject(FormBuilder);

  readonly incidents = signal<Incident[]>([]);
  readonly creating = signal(false);

  readonly form = this.fb.nonNullable.group({
    title: ['', Validators.required],
    description: ['', Validators.required],
    severity: ['Minor' as IncidentSeverity, Validators.required],
    affectedComponents: [['Api'] as PlatformComponentName[], Validators.required],
  });

  constructor(private readonly incidentsService: IncidentsService) {}

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.incidentsService.list().subscribe((incidents) => this.incidents.set(incidents));
  }

  create(): void {
    if (this.form.invalid) return;
    this.incidentsService.create(this.form.getRawValue()).subscribe(() => {
      this.form.reset({ severity: 'Minor', affectedComponents: ['Api'] });
      this.creating.set(false);
      this.reload();
    });
  }

  postUpdate(incident: Incident, status: IncidentStatus): void {
    const message = prompt(`Update message for "${incident.title}":`);
    if (!message) return;
    this.incidentsService.postUpdate(incident.id, status, message).subscribe(() => this.reload());
  }
}
