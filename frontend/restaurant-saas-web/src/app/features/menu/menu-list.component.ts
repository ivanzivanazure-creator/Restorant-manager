import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { AuthService } from '../../core/auth/auth.service';
import { MenuCategory } from '../../core/models/domain.models';
import { Permissions } from '../../core/models/permissions';
import { LocationContextService } from '../../core/services/location-context.service';
import { EmptyStateComponent } from '../../shared/components/empty-state.component';
import { MenuService } from './menu.service';

@Component({
  selector: 'rsaas-menu-list',
  standalone: true,
  imports: [
    CurrencyPipe,
    ReactiveFormsModule,
    MatExpansionModule,
    MatIconModule,
    MatChipsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule,
    EmptyStateComponent,
  ],
  templateUrl: './menu-list.component.html',
})
export class MenuListComponent implements OnInit {
  readonly categories = signal<MenuCategory[]>([]);
  readonly loading = signal(true);
  readonly addingCategory = signal(false);

  readonly categoryForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
  });

  readonly canManage = this.auth.hasPermission(Permissions.Menu.Manage);

  constructor(
    private readonly fb: FormBuilder,
    private readonly menuService: MenuService,
    private readonly locationContext: LocationContextService,
    private readonly auth: AuthService,
  ) {}

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    const locationId = this.locationContext.locationId();
    if (!locationId) {
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.menuService.getMenu(locationId).subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  addCategory(): void {
    const locationId = this.locationContext.locationId();
    if (!locationId || this.categoryForm.invalid) return;

    this.menuService
      .createCategory(locationId, { name: this.categoryForm.getRawValue().name, sortOrder: this.categories().length })
      .subscribe(() => {
        this.categoryForm.reset();
        this.addingCategory.set(false);
        this.reload();
      });
  }

  toggleProduct(productId: string, isActive: boolean): void {
    this.menuService.setProductActive(productId, isActive).subscribe(() => this.reload());
  }
}
