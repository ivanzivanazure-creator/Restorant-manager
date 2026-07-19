# Entity-Relationship Diagram

Full data model across all 16 modules. Domain entities exist in `RestaurantSaaS.Domain` for every
box below; see [`ROADMAP.md`](ROADMAP.md) for which ones have CQRS/API/UI built on top yet.
Split into sub-diagrams for readability — all sub-diagrams share the same entities.

## 1. Tenancy, Identity, Subscription

```mermaid
erDiagram
    SUPER_ADMIN ||--o{ RESTAURANT_OWNER : manages
    RESTAURANT_OWNER ||--o{ RESTAURANT : owns
    RESTAURANT ||--o{ LOCATION : has
    LOCATION ||--o{ DEPARTMENT : has
    DEPARTMENT ||--o{ EMPLOYEE : has
    RESTAURANT_OWNER ||--|| SUBSCRIPTION : has
    SUBSCRIPTION }o--|| PACKAGE : subscribes_to
    SUBSCRIPTION ||--o{ INVOICE : generates
    APPLICATION_USER ||--o{ REFRESH_TOKEN : has
    APPLICATION_USER ||--o| MFA_ENROLLMENT : has
    APPLICATION_USER }o--o{ ROLE : assigned
    ROLE }o--o{ PERMISSION : grants
    APPLICATION_USER ||--o| EMPLOYEE : "is (if staff)"
    APPLICATION_USER ||--o| RESTAURANT_OWNER : "is (if owner)"

    RESTAURANT_OWNER {
        guid Id PK
        string CompanyName
        string ContactEmail
        enum Status "Trial|Active|Suspended|Cancelled"
    }
    SUBSCRIPTION {
        guid Id PK
        guid RestaurantOwnerId FK
        guid PackageId FK
        enum BillingCycle "Monthly|Yearly"
        datetime TrialEndsAt
        datetime CurrentPeriodEnd
        enum Status "Trialing|Active|PastDue|Locked|Cancelled"
        string StripeSubscriptionId
    }
    PACKAGE {
        guid Id PK
        string Name "Starter|Professional|Unlimited"
        int MaxUsers "nullable = unlimited"
        int MaxLocations
        decimal MonthlyPrice
        decimal YearlyPrice
        json FeatureFlags
    }
    INVOICE {
        guid Id PK
        guid SubscriptionId FK
        string Number
        decimal Amount
        enum Status "Draft|Sent|Paid|Overdue"
        datetime IssuedAt
    }
```

## 2. Restaurant, Locations, Tables

```mermaid
erDiagram
    RESTAURANT ||--o{ LOCATION : has
    LOCATION ||--o{ TABLE : has
    LOCATION ||--o{ WORKING_HOUR : has
    LOCATION ||--|| TAX_CONFIG : has
    LOCATION ||--o{ WAREHOUSE : has
    TABLE ||--o| QR_CODE : has
    TABLE }o--o| ROOM : "linked (hotel)"

    LOCATION {
        guid Id PK
        guid RestaurantId FK
        guid TenantId FK
        string Name
        string Address
        string Currency
        string[] SupportedLanguages
        geo Coordinates
    }
    TABLE {
        guid Id PK
        guid LocationId FK
        string Label
        int Capacity
        float LayoutX
        float LayoutY
        enum Shape "Round|Square|Rect"
        enum Status "Free|Occupied|Reserved|Cleaning"
    }
```

## 3. Menu

```mermaid
erDiagram
    LOCATION ||--o{ MENU_CATEGORY : has
    MENU_CATEGORY ||--o{ PRODUCT : contains
    PRODUCT ||--o{ PRODUCT_VARIANT : has
    PRODUCT ||--o{ PRODUCT_MODIFIER_GROUP : has
    PRODUCT_MODIFIER_GROUP ||--o{ MODIFIER : has
    PRODUCT ||--o{ ALLERGEN_LINK : flags
    PRODUCT ||--o| NUTRITION_INFO : has
    PRODUCT ||--o| RECIPE : "produced by"
    PRODUCT ||--o{ MENU_AVAILABILITY_RULE : has

    PRODUCT {
        guid Id PK
        guid CategoryId FK
        string Name
        decimal BasePrice
        bool IsHappyHour
        bool IsSeasonal
        string ImageUrl
        bool IsActive
    }
```

## 4. POS & Payments

```mermaid
erDiagram
    TABLE ||--o{ ORDER : opens
    ORDER ||--o{ ORDER_ITEM : contains
    ORDER_ITEM }o--|| PRODUCT_VARIANT : references
    ORDER_ITEM ||--o{ ORDER_ITEM_MODIFIER : has
    ORDER ||--o{ PAYMENT : "settled by"
    ORDER ||--o{ DISCOUNT_APPLICATION : has
    ORDER ||--o| REFUND : may_have
    ORDER }o--o| GUEST_STAY : "room charge (hotel)"

    ORDER {
        guid Id PK
        guid LocationId FK
        guid TableId FK
        guid TenantId FK
        enum Status "Open|InKitchen|Ready|Served|Paid|Cancelled"
        decimal Subtotal
        decimal TaxTotal
        decimal TipTotal
        decimal GrandTotal
    }
    PAYMENT {
        guid Id PK
        guid OrderId FK
        enum Method "Cash|Card|Voucher|RoomCharge"
        decimal Amount
        enum Status "Pending|Captured|Refunded|Failed"
    }
```

## 5. Kitchen Display

```mermaid
erDiagram
    ORDER ||--o{ KITCHEN_TICKET : generates
    KITCHEN_TICKET ||--o{ KITCHEN_TICKET_ITEM : contains
    KITCHEN_TICKET_ITEM }o--|| ORDER_ITEM : references
    KITCHEN_TICKET {
        guid Id PK
        guid OrderId FK
        guid LocationId FK
        enum Priority "Normal|Rush|VIP"
        enum Status "Queued|InProgress|Ready|Served"
        datetime StartedAt
        datetime ReadyAt
        int TargetCookMinutes
    }
```

## 6. Inventory, Recipes, Procurement

```mermaid
erDiagram
    LOCATION ||--o{ WAREHOUSE : has
    WAREHOUSE ||--o{ STOCK_LEVEL : tracks
    STOCK_LEVEL }o--|| INGREDIENT : "of"
    STOCK_LEVEL ||--o{ STOCK_BATCH : "FIFO batches"
    WAREHOUSE ||--o{ STOCK_MOVEMENT : logs
    WAREHOUSE ||--o{ STOCK_TRANSFER : "src/dst"
    INGREDIENT ||--o{ RECIPE_INGREDIENT : "used in"
    RECIPE ||--o{ RECIPE_INGREDIENT : requires
    RECIPE ||--o{ RECIPE_STEP : has
    RECIPE ||--o{ RECIPE_VERSION : versioned_as
    SUPPLIER ||--o{ PURCHASE_ORDER : receives
    PURCHASE_ORDER ||--o{ PURCHASE_ORDER_LINE : contains
    PURCHASE_ORDER ||--o| GOODS_RECEIPT : fulfilled_by
    SUPPLIER ||--o{ PRICE_LIST : publishes
    INGREDIENT ||--o{ WASTE_RECORD : "wasted as"

    INGREDIENT {
        guid Id PK
        string Name
        string Unit
        decimal ReorderThreshold
        decimal CostPerUnit
        string Barcode
    }
    STOCK_BATCH {
        guid Id PK
        guid StockLevelId FK
        decimal Quantity
        datetime ReceivedAt
        datetime ExpiresAt
    }
    RECIPE {
        guid Id PK
        guid ProductId FK
        int CurrentVersion
        int PrepMinutes
        int CookMinutes
        decimal CostPerServing "computed"
    }
```

## 7. Hotel Module

```mermaid
erDiagram
    LOCATION ||--o{ ROOM : has
    ROOM ||--o{ RESERVATION : booked_via
    GUEST ||--o{ RESERVATION : makes
    RESERVATION ||--o| GUEST_STAY : "checked-in as"
    GUEST_STAY ||--o{ ROOM_SERVICE_ORDER : requests
    GUEST_STAY ||--o{ MINIBAR_CHARGE : incurs
    GUEST_STAY ||--o{ ORDER : "table linked to room"
    GUEST ||--o{ GUEST_STAY : "history"

    ROOM {
        guid Id PK
        guid LocationId FK
        string Number
        enum Type "Standard|Suite|Deluxe"
        decimal NightlyRate
        enum Status "Vacant|Occupied|Cleaning|OutOfService"
    }
    GUEST_STAY {
        guid Id PK
        guid ReservationId FK
        datetime CheckInAt
        datetime CheckOutAt
        decimal RoomChargeBalance
    }
```

## 8. CRM

```mermaid
erDiagram
    CUSTOMER ||--o{ ORDER : places
    CUSTOMER ||--o| LOYALTY_ACCOUNT : has
    LOYALTY_ACCOUNT ||--o{ LOYALTY_TRANSACTION : logs
    CUSTOMER ||--o{ COUPON_REDEMPTION : redeems
    COUPON ||--o{ COUPON_REDEMPTION : "redeemed via"
    CUSTOMER ||--o{ GIFT_CARD : owns
    CUSTOMER ||--o{ FEEDBACK : submits
    ORDER ||--o| FEEDBACK : "reviewed via"
```

## 9. Employees

```mermaid
erDiagram
    EMPLOYEE }o--|| DEPARTMENT : belongs_to
    EMPLOYEE ||--o{ SHIFT : scheduled
    EMPLOYEE ||--o{ ATTENDANCE_RECORD : clocks
    EMPLOYEE ||--o{ PAYROLL_ENTRY : "paid via"
    PAYROLL_ENTRY ||--o{ BONUS : includes
    EMPLOYEE ||--o{ LEAVE_REQUEST : requests
    EMPLOYEE ||--o{ PERFORMANCE_REVIEW : reviewed_by
```

## 10. Cross-cutting

```mermaid
erDiagram
    APPLICATION_USER ||--o{ AUDIT_LOG : "acts, logged as"
    RESTAURANT_OWNER ||--o{ AUDIT_LOG : scoped_to
    RESTAURANT_OWNER ||--o{ NOTIFICATION : receives
    EMPLOYEE ||--o{ NOTIFICATION : receives
```

All tenant-scoped tables (everything below `RESTAURANT_OWNER`) carry a `TenantId` column mapping
to `RestaurantOwnerId`, enforced by the EF Core global query filter described in
[`ARCHITECTURE.md`](ARCHITECTURE.md#multi-tenancy).
