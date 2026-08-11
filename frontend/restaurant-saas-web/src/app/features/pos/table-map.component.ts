import { DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { Order, RestaurantTable } from '../../core/models/domain.models';
import { LocationContextService } from '../../core/services/location-context.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { PosService } from './pos.service';

@Component({
  selector: 'rsaas-table-map',
  standalone: true,
  imports: [DecimalPipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule, EmptyStateComponent],
  templateUrl: './table-map.component.html',
  styleUrl: './table-map.component.scss',
})
export class TableMapComponent implements OnInit {
  readonly tables = signal<RestaurantTable[]>([]);
  readonly openOrdersByTable = signal<Map<string, Order>>(new Map());
  readonly loading = signal(true);

  constructor(
    private readonly posService: PosService,
    private readonly locationContext: LocationContextService,
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    const locationId = this.locationContext.locationId();
    if (!locationId) {
      this.loading.set(false);
      return;
    }

    forkJoin({
      tables: this.posService.getTables(locationId),
      orders: this.posService.getOpenOrders(locationId),
    }).subscribe({
      next: ({ tables, orders }) => {
        this.tables.set(tables);
        const byTable = new Map<string, Order>();
        for (const order of orders) {
          if (order.tableId) byTable.set(order.tableId, order);
        }
        this.openOrdersByTable.set(byTable);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  async selectTable(table: RestaurantTable): Promise<void> {
    const existing = this.openOrdersByTable().get(table.id);
    if (existing) {
      this.router.navigate(['/pos/order', existing.id]);
      return;
    }

    const locationId = this.locationContext.locationId();
    const userId = this.auth.currentUser()?.userId;
    if (!locationId || !userId) return;

    this.posService.openOrder(locationId, table.id, userId).subscribe((orderId) => {
      this.router.navigate(['/pos/order', orderId]);
    });
  }
}
