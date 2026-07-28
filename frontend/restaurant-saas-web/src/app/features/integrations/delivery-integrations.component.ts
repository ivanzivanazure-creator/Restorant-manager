import { DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { DeliveryIntegration, DeliveryPlatform, RegisterDeliveryIntegrationResponse } from '../../core/models/domain.models';
import { LocationContextService } from '../../core/services/location-context.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { IntegrationsService } from './integrations.service';

@Component({
  selector: 'rsaas-delivery-integrations',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    EmptyStateComponent,
  ],
  templateUrl: './delivery-integrations.component.html',
})
export class DeliveryIntegrationsComponent implements OnInit {
  readonly platforms: DeliveryPlatform[] = ['UberEats', 'DoorDash', 'GrubHub', 'Deliveroo', 'Other'];
  readonly integrations = signal<DeliveryIntegration[]>([]);
  readonly adding = signal(false);
  readonly justRegistered = signal<RegisterDeliveryIntegrationResponse | null>(null);

  readonly form = this.fb.nonNullable.group({
    platform: ['UberEats' as DeliveryPlatform, Validators.required],
    externalStoreId: [''],
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly integrationsService: IntegrationsService,
    private readonly locationContext: LocationContextService,
  ) {}

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.integrationsService.list().subscribe((integrations) => this.integrations.set(integrations));
  }

  register(): void {
    const locationId = this.locationContext.locationId();
    if (!locationId || this.form.invalid) return;

    const { platform, externalStoreId } = this.form.getRawValue();
    this.integrationsService.register({ locationId, platform, externalStoreId: externalStoreId || null }).subscribe((result) => {
      this.justRegistered.set(result);
      this.adding.set(false);
      this.form.reset({ platform: 'UberEats' });
      this.reload();
    });
  }

  deactivate(integration: DeliveryIntegration): void {
    this.integrationsService.deactivate(integration.id).subscribe(() => this.reload());
  }
}
