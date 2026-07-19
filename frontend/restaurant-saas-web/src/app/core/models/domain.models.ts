// Mirrors the DTOs exposed by the .NET API (see backend/src/Core/RestaurantSaaS.Application).

export interface Restaurant {
  id: string;
  name: string;
  legalName: string;
  defaultCurrency: string;
  isActive: boolean;
}

export interface Location {
  id: string;
  restaurantId: string;
  name: string;
  city: string;
  country: string;
  currency: string;
  isActive: boolean;
}

export type TableShape = 'Round' | 'Square' | 'Rectangle';
export type TableStatus = 'Free' | 'Occupied' | 'Reserved' | 'Cleaning' | 'OutOfService';

export interface RestaurantTable {
  id: string;
  label: string;
  capacity: number;
  shape: TableShape;
  status: TableStatus;
  x: number;
  y: number;
  qrCodeImageDataUri: string | null;
}

export interface Modifier {
  id: string;
  name: string;
  priceDelta: number;
  isActive: boolean;
}

export interface ModifierGroup {
  id: string;
  name: string;
  isRequired: boolean;
  maxSelections: number;
  modifiers: Modifier[];
}

export interface Variant {
  id: string;
  name: string;
  priceDelta: number;
  isDefault: boolean;
  isActive: boolean;
}

export interface Product {
  id: string;
  name: string;
  description: string;
  basePrice: number;
  currency: string;
  imageUrl: string | null;
  isActive: boolean;
  isHappyHour: boolean;
  isSeasonal: boolean;
  allergens: string[];
  variants: Variant[];
  modifierGroups: ModifierGroup[];
}

export interface MenuCategory {
  id: string;
  name: string;
  sortOrder: number;
  products: Product[];
}

export type OrderStatus = 'Open' | 'InKitchen' | 'ReadyToServe' | 'Served' | 'PartiallyPaid' | 'Paid' | 'Cancelled';
export type OrderSource = 'Pos' | 'QrSelfOrder' | 'RoomService' | 'ThirdPartyDelivery';
export type PaymentMethod = 'Cash' | 'Card' | 'Voucher' | 'RoomCharge' | 'GiftCard' | 'MobileWallet';
export type PaymentStatus = 'Pending' | 'Captured' | 'PartiallyRefunded' | 'Refunded' | 'Failed';

export interface OrderItemModifier {
  name: string;
  priceDelta: number;
}

export interface OrderItem {
  id: string;
  productVariantId: string;
  productName: string;
  variantName: string;
  unitPrice: number;
  quantity: number;
  notes: string | null;
  lineTotal: number;
  modifiers: OrderItemModifier[];
}

export interface Payment {
  id: string;
  method: PaymentMethod;
  amount: number;
  status: PaymentStatus;
  reference: string | null;
}

export interface Order {
  id: string;
  locationId: string;
  tableId: string | null;
  status: OrderStatus;
  source: OrderSource;
  currency: string;
  subtotal: number;
  discountTotal: number;
  taxTotal: number;
  tipAmount: number;
  grandTotal: number;
  amountPaid: number;
  amountDue: number;
  openedAt: string;
  items: OrderItem[];
  payments: Payment[];
}

export type KitchenTicketStatus = 'Queued' | 'InProgress' | 'Ready' | 'Served' | 'Cancelled';
export type KitchenTicketPriority = 'Normal' | 'Rush' | 'Vip';

export interface KitchenTicketItem {
  id: string;
  productName: string;
  variantName: string;
  quantity: number;
  notes: string | null;
  status: KitchenTicketStatus;
}

export interface KitchenTicket {
  id: string;
  orderId: string;
  tableLabel: string | null;
  status: KitchenTicketStatus;
  priority: KitchenTicketPriority;
  targetCookMinutes: number;
  queuedAt: string;
  startedAt: string | null;
  isOverdue: boolean;
  items: KitchenTicketItem[];
}

export interface StockLevel {
  ingredientId: string;
  ingredientName: string;
  warehouseId: string;
  warehouseName: string;
  quantityOnHand: number;
  unit: string;
  reorderThreshold: number;
  isBelowThreshold: boolean;
}

export interface KitchenStatusSummary {
  queued: number;
  inProgress: number;
  ready: number;
  overdue: number;
}

export interface InventoryAlert {
  ingredientId: string;
  ingredientName: string;
  quantityOnHand: number;
  reorderThreshold: number;
}

export interface DashboardSummary {
  todayRevenue: number;
  todayOrderCount: number;
  todayAverageOrderValue: number;
  last7DaysRevenue: number;
  kitchen: KitchenStatusSummary;
  inventoryAlerts: InventoryAlert[];
}

export interface Package {
  id: string;
  name: string;
  maxUsers: number | null;
  maxLocations: number;
  monthlyPrice: number;
  yearlyPrice: number;
  isActive: boolean;
}

export interface TenantSummary {
  id: string;
  companyName: string;
  contactEmail: string;
  status: 'Trial' | 'Active' | 'Suspended' | 'Cancelled';
  packageName: string;
  subscriptionStatus: 'Trialing' | 'Active' | 'PastDue' | 'Locked' | 'Cancelled';
  currentPeriodEnd: string;
  restaurantCount: number;
}

export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface PlatformAnalytics {
  totalTenants: number;
  activeTenants: number;
  trialingTenants: number;
  lockedTenants: number;
  monthlyRecurringRevenue: number;
}
