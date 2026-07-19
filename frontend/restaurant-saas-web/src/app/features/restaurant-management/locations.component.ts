import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Location, RestaurantTable } from '../../core/models/domain.models';
import { LocationContextService } from '../../core/services/location-context.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { RestaurantManagementService } from './restaurant-management.service';

@Component({
  selector: 'rsaas-locations',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, EmptyStateComponent],
  templateUrl: './locations.component.html',
})
export class LocationsComponent implements OnInit {
  readonly locations = signal<Location[]>([]);
  readonly tables = signal<RestaurantTable[]>([]);
  readonly loading = signal(true);
  readonly addingTable = signal(false);

  readonly tableForm = this.fb.nonNullable.group({
    label: ['', Validators.required],
    capacity: [2, [Validators.required, Validators.min(1)]],
  });

  constructor(
    private readonly fb: FormBuilder,
    private readonly restaurantManagementService: RestaurantManagementService,
    readonly locationContext: LocationContextService,
  ) {}

  ngOnInit(): void {
    this.restaurantManagementService.getLocations().subscribe({
      next: (locations) => {
        this.locations.set(locations);
        this.loading.set(false);
        const active = this.locationContext.locationId();
        if (active) this.loadTables(active);
      },
      error: () => this.loading.set(false),
    });
  }

  selectLocation(location: Location): void {
    this.locationContext.setLocation(location.id);
    this.loadTables(location.id);
  }

  loadTables(locationId: string): void {
    this.restaurantManagementService.getTables(locationId).subscribe((tables) => this.tables.set(tables));
  }

  addTable(): void {
    const locationId = this.locationContext.locationId();
    if (!locationId || this.tableForm.invalid) return;

    const index = this.tables().length;
    const { label, capacity } = this.tableForm.getRawValue();

    this.restaurantManagementService
      .createTable(locationId, { label, capacity, shape: 'Round', x: (index % 5) * 120, y: Math.floor(index / 5) * 120 })
      .subscribe(() => {
        this.tableForm.reset({ capacity: 2 });
        this.addingTable.set(false);
        this.loadTables(locationId);
      });
  }

  generateQr(table: RestaurantTable): void {
    this.restaurantManagementService.generateQrCode(table.id, window.location.origin).subscribe(() => {
      const locationId = this.locationContext.locationId();
      if (locationId) this.loadTables(locationId);
    });
  }
}
