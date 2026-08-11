import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { KitchenTicket, KitchenTicketPriority } from '../../core/models/domain.models';
import { LocationContextService } from '../../core/services/location-context.service';
import { SignalRHubClient } from '../../core/services/signalr.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { KitchenService } from './kitchen.service';

@Component({
  selector: 'rsaas-kitchen-display',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatMenuModule, MatProgressSpinnerModule, EmptyStateComponent],
  templateUrl: './kitchen-display.component.html',
  styleUrl: './kitchen-display.component.scss',
})
export class KitchenDisplayComponent implements OnInit, OnDestroy {
  readonly tickets = signal<KitchenTicket[]>([]);
  readonly loading = signal(true);

  private connection: signalR.HubConnection | null = null;

  constructor(
    private readonly kitchenService: KitchenService,
    private readonly locationContext: LocationContextService,
    private readonly hubClient: SignalRHubClient,
  ) {}

  ngOnInit(): void {
    this.reload();

    const locationId = this.locationContext.locationId();
    if (!locationId) return;

    this.connection = this.hubClient.connect(environment.kitchenHubUrl);
    this.hubClient.joinLocation(this.connection, locationId).catch(() => void 0);
    this.hubClient.onEvent(this.connection, 'kitchenEvent').subscribe(() => this.reload());
  }

  ngOnDestroy(): void {
    this.connection?.stop();
  }

  reload(): void {
    const locationId = this.locationContext.locationId();
    if (!locationId) {
      this.loading.set(false);
      return;
    }

    this.kitchenService.getQueue(locationId).subscribe({
      next: (tickets) => {
        this.tickets.set(tickets);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  start(ticket: KitchenTicket): void {
    this.kitchenService.start(ticket.id).subscribe(() => this.reload());
  }

  markReady(ticket: KitchenTicket): void {
    this.kitchenService.markReady(ticket.id).subscribe(() => this.reload());
  }

  markServed(ticket: KitchenTicket): void {
    this.kitchenService.markServed(ticket.id).subscribe(() => this.reload());
  }

  setPriority(ticket: KitchenTicket, priority: KitchenTicketPriority): void {
    this.kitchenService.setPriority(ticket.id, priority).subscribe(() => this.reload());
  }
}
