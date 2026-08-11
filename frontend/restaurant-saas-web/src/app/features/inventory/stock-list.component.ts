import { DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { StockLevel } from '../../core/models/domain.models';
import { LocationContextService } from '../../core/services/location-context.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { InventoryService, Warehouse } from './inventory.service';

@Component({
  selector: 'rsaas-stock-list',
  standalone: true,
  imports: [DecimalPipe, FormsModule, MatChipsModule, MatIconModule, MatSelectModule, MatProgressSpinnerModule, EmptyStateComponent],
  templateUrl: './stock-list.component.html',
})
export class StockListComponent implements OnInit {
  readonly warehouses = signal<Warehouse[]>([]);
  readonly selectedWarehouseId = signal<string | null>(null);
  readonly stock = signal<StockLevel[]>([]);
  readonly loading = signal(true);

  constructor(
    private readonly inventoryService: InventoryService,
    private readonly locationContext: LocationContextService,
  ) {}

  ngOnInit(): void {
    const locationId = this.locationContext.locationId();
    if (!locationId) {
      this.loading.set(false);
      return;
    }

    this.inventoryService.getWarehouses(locationId).subscribe({
      next: (warehouses) => {
        this.warehouses.set(warehouses);
        if (warehouses.length > 0) {
          this.selectWarehouse(warehouses[0].id);
        } else {
          this.loading.set(false);
        }
      },
      error: () => this.loading.set(false),
    });
  }

  selectWarehouse(warehouseId: string): void {
    this.selectedWarehouseId.set(warehouseId);
    this.loading.set(true);
    this.inventoryService.getStock(warehouseId).subscribe({
      next: (stock) => {
        this.stock.set(stock);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
