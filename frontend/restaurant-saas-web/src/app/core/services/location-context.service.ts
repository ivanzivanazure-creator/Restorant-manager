import { Injectable, computed, signal } from '@angular/core';
import { AuthService } from '../auth/auth.service';

const STORAGE_KEY = 'rsaas.locationId';

/** Tracks which Location the staff member is currently operating at (POS/KDS/Inventory are all
 * scoped to one location at a time). Defaults to the JWT's location_id claim if present. */
@Injectable({ providedIn: 'root' })
export class LocationContextService {
  private readonly locationIdSignal = signal<string | null>(localStorage.getItem(STORAGE_KEY));

  readonly locationId = this.locationIdSignal.asReadonly();
  readonly hasLocation = computed(() => this.locationIdSignal() !== null);

  constructor(private readonly auth: AuthService) {
    const claimLocation = this.auth.currentUser()?.locationId;
    if (!this.locationIdSignal() && claimLocation) this.setLocation(claimLocation);
  }

  setLocation(locationId: string): void {
    localStorage.setItem(STORAGE_KEY, locationId);
    this.locationIdSignal.set(locationId);
  }

  clear(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.locationIdSignal.set(null);
  }
}
