import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { ActivatedRoute, Router } from '@angular/router';
import { MenuCategory, Order, PaymentMethod } from '../../core/models/domain.models';
import { LocationContextService } from '../../core/services/location-context.service';
import { MenuService } from '../menu/menu.service';
import { PosService } from './pos.service';

@Component({
  selector: 'rsaas-order-screen',
  standalone: true,
  imports: [
    CurrencyPipe,
    FormsModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatIconModule,
    MatDividerModule,
    MatFormFieldModule,
    MatInputModule,
    MatTabsModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './order-screen.component.html',
  styleUrl: './order-screen.component.scss',
})
export class OrderScreenComponent implements OnInit {
  readonly order = signal<Order | null>(null);
  readonly menu = signal<MenuCategory[]>([]);
  readonly loading = signal(true);
  readonly tipAmount = signal(0);
  readonly paymentMethod = signal<PaymentMethod>('Cash');

  private orderId = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly posService: PosService,
    private readonly menuService: MenuService,
    private readonly locationContext: LocationContextService,
    private readonly snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.orderId = this.route.snapshot.paramMap.get('orderId')!;
    this.reload();

    const locationId = this.locationContext.locationId();
    if (locationId) {
      this.menuService.getMenu(locationId, true).subscribe((categories) => this.menu.set(categories));
    }
  }

  reload(): void {
    this.loading.set(true);
    this.posService.getOrder(this.orderId).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  addProduct(productId: string): void {
    const category = this.menu().find((c) => c.products.some((p) => p.id === productId));
    const product = category?.products.find((p) => p.id === productId);
    const defaultVariant = product?.variants.find((v) => v.isDefault) ?? product?.variants[0];
    if (!defaultVariant) return;

    this.posService.addItem(this.orderId, { productVariantId: defaultVariant.id, quantity: 1, notes: null, modifiers: [] }).subscribe(() =>
      this.reload(),
    );
  }

  removeItem(orderItemId: string): void {
    this.posService.removeItem(this.orderId, orderItemId).subscribe(() => this.reload());
  }

  applyTip(): void {
    if (this.tipAmount() <= 0) return;
    this.posService.addTip(this.orderId, this.tipAmount()).subscribe(() => this.reload());
  }

  sendToKitchen(): void {
    // In a full flow the waiter picks the warehouse; the demo tenant has exactly one per location,
    // so a real deployment would resolve this via a warehouse-selection step here.
    const warehouseId = prompt('Warehouse ID to deduct ingredients from:');
    if (!warehouseId) return;

    this.posService.sendToKitchen(this.orderId, warehouseId).subscribe({
      next: () => {
        this.snackBar.open('Sent to kitchen', 'Dismiss', { duration: 3000 });
        this.reload();
      },
      error: () => this.snackBar.open('Could not send to kitchen', 'Dismiss', { duration: 3000 }),
    });
  }

  pay(): void {
    const order = this.order();
    if (!order) return;

    this.posService.pay(this.orderId, this.paymentMethod(), order.amountDue).subscribe({
      next: () => {
        this.snackBar.open('Payment recorded', 'Dismiss', { duration: 3000 });
        this.router.navigate(['/pos']);
      },
      error: () => this.snackBar.open('Payment failed', 'Dismiss', { duration: 3000 }),
    });
  }
}
