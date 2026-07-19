import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { Permissions } from '../../core/models/permissions';
import { ThemeService } from '../../core/services/theme.service';

interface NavItem {
  label: string;
  icon: string;
  path: string;
  permission?: string;
  superAdminOnly?: boolean;
}

@Component({
  selector: 'rsaas-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatTooltipModule,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  readonly navItems: NavItem[] = [
    { label: 'Dashboard', icon: 'dashboard', path: '/dashboard', permission: Permissions.Dashboard.View },
    { label: 'Point of Sale', icon: 'point_of_sale', path: '/pos', permission: Permissions.Pos.ViewOrders },
    { label: 'Kitchen Display', icon: 'soup_kitchen', path: '/kitchen-display', permission: Permissions.Kitchen.ViewQueue },
    { label: 'Menu', icon: 'restaurant_menu', path: '/menu', permission: Permissions.Menu.View },
    { label: 'Inventory', icon: 'inventory_2', path: '/inventory', permission: Permissions.Inventory.View },
    { label: 'Restaurant', icon: 'storefront', path: '/restaurant-management', permission: Permissions.Tenancy.ManageLocations },
    { label: 'Super Admin', icon: 'admin_panel_settings', path: '/super-admin', superAdminOnly: true },
  ];

  constructor(
    readonly auth: AuthService,
    readonly theme: ThemeService,
  ) {}

  visible(item: NavItem): boolean {
    const user = this.auth.currentUser();
    if (!user) return false;
    if (item.superAdminOnly) return user.isSuperAdmin;
    return !item.permission || this.auth.hasPermission(item.permission);
  }

  logout(): void {
    this.auth.logout();
  }
}
