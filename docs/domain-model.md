# Domain Model — Chairly

## Overview

Chairly is a multi-tenant SaaS platform for salons and barbershops. Each tenant represents one salon location. All data is tenant-scoped.

---

## Bounded Contexts

### 1. Bookings

The core scheduling context. A **Booking** represents a scheduled visit by a client with a staff member, containing one or more services.

**Entities:**

- **Booking** (Aggregate Root)
  - `Id` (Guid)
  - `TenantId` (Guid)
  - `ClientId` (Guid)
  - `StaffMemberId` (Guid)
  - `StartTime` (DateTimeOffset)
  - `EndTime` (DateTimeOffset) — calculated from sum of service durations
  - `Notes` (string, optional)
  - `BookingServices` (List\<BookingService\>)
  - `CreatedAtUtc` (DateTimeOffset), `CreatedBy` (Guid)
  - `UpdatedAtUtc` (DateTimeOffset, optional), `UpdatedBy` (Guid, optional)
  - `ConfirmedAtUtc` (DateTimeOffset, optional), `ConfirmedBy` (Guid, optional)
  - `StartedAtUtc` (DateTimeOffset, optional), `StartedBy` (Guid, optional)
  - `CompletedAtUtc` (DateTimeOffset, optional), `CompletedBy` (Guid, optional)
  - `CancelledAtUtc` (DateTimeOffset, optional), `CancelledBy` (Guid, optional)
  - `NoShowAtUtc` (DateTimeOffset, optional), `NoShowBy` (Guid, optional)

- **BookingService** (Value Object)
  - `ServiceId` (Guid)
  - `ServiceName` (string) — snapshot at time of booking
  - `Duration` (TimeSpan)
  - `Price` (decimal) — snapshot at time of booking
  - `SortOrder` (int)

**Derived Status (no status column — derived from timestamps):**
- **Scheduled**: `CreatedAtUtc` is set, no other timestamps
- **Confirmed**: `ConfirmedAtUtc` is set
- **InProgress**: `StartedAtUtc` is set
- **Completed**: `CompletedAtUtc` is set
- **Cancelled**: `CancelledAtUtc` is set
- **NoShow**: `NoShowAtUtc` is set

**Business Rules:**
- A booking must have at least one service
- Staff member cannot have overlapping bookings
- EndTime = StartTime + sum of all service durations
- Cancellation is only allowed if `CompletedAtUtc` is null (not yet completed)
- Terminal states: once `CompletedAtUtc`, `CancelledAtUtc`, or `NoShowAtUtc` is set, no further state changes
- Price and service name are snapshotted at booking creation (not affected by later catalog changes)

---

### 2. Clients

Manages client information and history.

**Entities:**

- **Client** (Aggregate Root)
  - `Id` (Guid)
  - `TenantId` (Guid)
  - `FirstName` (string)
  - `LastName` (string)
  - `Email` (string, optional)
  - `PhoneNumber` (string, optional)
  - `Notes` (string, optional)
  - `CreatedAtUtc` (DateTimeOffset)
  - `UpdatedAtUtc` (DateTimeOffset, optional)

**Business Rules:**
- A client belongs to exactly one tenant
- At least one contact method (email or phone) should be provided
- Client can be soft-deleted (not removed, to preserve booking history)

- **Recipe** (Entity)
  - `Id` (Guid)
  - `TenantId` (Guid)
  - `BookingId` (Guid) — the booking this recipe was created for
  - `ClientId` (Guid)
  - `StaffMemberId` (Guid)
  - `Title` (string)
  - `Notes` (string, optional)
  - `Products` (List\<RecipeProduct\>)
  - `CreatedAtUtc` (DateTimeOffset), `CreatedBy` (Guid)
  - `UpdatedAtUtc` (DateTimeOffset, optional), `UpdatedBy` (Guid, optional)

- **RecipeProduct** (Value Object)
  - `Id` (Guid)
  - `Name` (string)
  - `Brand` (string, optional)
  - `Quantity` (string, optional)
  - `SortOrder` (int)

**Business Rules (Recipes):**
- A recipe is linked to a specific completed booking
- Records what products and techniques were used during a visit
- Staff can review a client's recipe history before their next booking

---

### 3. Staff

Manages staff members, their roles, and working hours.

**Entities:**

- **StaffMember** (Aggregate Root)
  - `Id` (Guid)
  - `TenantId` (Guid)
  - `UserId` (Guid) — link to identity/auth user
  - `FirstName` (string)
  - `LastName` (string)
  - `Email` (string)
  - `PhoneNumber` (string, optional)
  - `Role` (StaffRole)
  - `IsActive` (bool)
  - `WorkingHours` (List\<WorkingHoursEntry\>)
  - `CreatedAtUtc` (DateTimeOffset)
  - `UpdatedAtUtc` (DateTimeOffset, optional)

- **WorkingHoursEntry** (Value Object)
  - `DayOfWeek` (DayOfWeek)
  - `StartTime` (TimeOnly)
  - `EndTime` (TimeOnly)

**Enums:**

- **StaffRole**: `Owner`, `Manager`, `StaffMember`

**Business Rules:**
- Each tenant must have exactly one Owner
- Owner and Manager can manage other staff members
- Working hours define when a staff member is available for bookings
- Staff member can be deactivated (not deleted, to preserve booking history)

---

### 4. Services

The catalog of services a salon offers.

**Entities:**

- **Service** (Aggregate Root)
  - `Id` (Guid)
  - `TenantId` (Guid)
  - `Name` (string)
  - `Description` (string, optional)
  - `Duration` (TimeSpan)
  - `Price` (decimal)
  - `VatRate` (decimal) — VAT percentage applied to this service
  - `CategoryId` (Guid, optional)
  - `IsActive` (bool)
  - `SortOrder` (int)
  - `CreatedAtUtc` (DateTimeOffset)
  - `UpdatedAtUtc` (DateTimeOffset, optional)

- **ServiceCategory** (Entity)
  - `Id` (Guid)
  - `TenantId` (Guid)
  - `Name` (string)
  - `SortOrder` (int)

**Business Rules:**
- Service name must be unique within a tenant
- Deactivating a service does not affect existing bookings
- Price is in the tenant's currency (currency is a tenant-level setting)

---

### 5. Billing

Invoicing and payment tracking after bookings are completed.

**Entities:**

- **Invoice** (Aggregate Root)
  - `Id` (Guid)
  - `TenantId` (Guid)
  - `BookingId` (Guid)
  - `ClientId` (Guid)
  - `InvoiceNumber` (string) — tenant-scoped sequential number
  - `InvoiceDate` (DateOnly)
  - `TotalAmount` (decimal)
  - `LineItems` (List\<InvoiceLineItem\>)
  - `CreatedAtUtc` (DateTimeOffset), `CreatedBy` (Guid)
  - `SentAtUtc` (DateTimeOffset, optional), `SentBy` (Guid, optional)
  - `PaidAtUtc` (DateTimeOffset, optional), `PaidBy` (Guid, optional)
  - `VoidedAtUtc` (DateTimeOffset, optional), `VoidedBy` (Guid, optional)

- **InvoiceLineItem** (Value Object)
  - `Description` (string)
  - `Quantity` (int)
  - `UnitPrice` (decimal)
  - `TotalPrice` (decimal)

**Derived Status (no status column — derived from timestamps):**
- **Draft**: `CreatedAtUtc` is set, no other timestamps
- **Sent**: `SentAtUtc` is set
- **Paid**: `PaidAtUtc` is set
- **Void**: `VoidedAtUtc` is set

**Business Rules:**
- Invoice is generated from a completed booking
- Line items are derived from the booking's services
- Invoice number is sequential per tenant
- Cannot be voided after `PaidAtUtc` is set

---

### 6. Notifications

Handles sending reminders and confirmations via email or SMS.

**Entities:**

- **Notification** (Aggregate Root)
  - `Id` (Guid)
  - `TenantId` (Guid)
  - `RecipientId` (Guid) — ClientId or StaffMemberId
  - `RecipientType` (RecipientType)
  - `Channel` (NotificationChannel)
  - `Type` (NotificationType)
  - `ReferenceId` (Guid) — e.g. BookingId
  - `ScheduledAtUtc` (DateTimeOffset)
  - `CreatedAtUtc` (DateTimeOffset)
  - `SentAtUtc` (DateTimeOffset, optional)
  - `FailedAtUtc` (DateTimeOffset, optional)
  - `FailureReason` (string, optional)

**Enums (kept — these are classification, not status):**

- **RecipientType**: `Client`, `StaffMember`
- **NotificationChannel**: `Email`, `Sms`
- **NotificationType**: `BookingConfirmation`, `BookingReminder`, `BookingCancellation`, `BookingReceived`, `InvoiceSent`

- **EmailTemplate**
  - `Id` (Guid)
  - `TenantId` (Guid)
  - `TemplateType` (NotificationType)
  - `Subject` (string, max 500)
  - `MainMessage` (string, max 2000)
  - `ClosingMessage` (string, max 1000)
  - `CreatedAtUtc` (DateTimeOffset)
  - `CreatedBy` (Guid)
  - `UpdatedAtUtc` (DateTimeOffset, optional)
  - `UpdatedBy` (Guid, optional)
  - Unique constraint on (TenantId, TemplateType) — at most one custom template per tenant per type
  - When no EmailTemplate row exists for a tenant + type, the system uses hardcoded defaults

**Derived Status (no status column — derived from timestamps):**
- **Pending**: `CreatedAtUtc` is set, `SentAtUtc` and `FailedAtUtc` are null
- **Sent**: `SentAtUtc` is set
- **Failed**: `FailedAtUtc` is set

**Business Rules:**
- Notifications are triggered by domain events (booking created, cancelled, etc.)
- Delivered asynchronously via RabbitMQ
- Retry logic for failed deliveries
- Each tenant can customize email templates (Subject, MainMessage, ClosingMessage) per NotificationType
- System falls back to hardcoded defaults when no custom template exists

---

### 7. Settings

Per-tenant configuration for company information and VAT.

**Entities:**

- **TenantSettings** (Entity — one per tenant)
  - `Id` (Guid)
  - `TenantId` (Guid)
  - `CompanyName` (string, optional)
  - `CompanyEmail` (string, optional)
  - `Street` (string, optional)
  - `HouseNumber` (string, optional)
  - `PostalCode` (string, optional)
  - `City` (string, optional)
  - `CompanyPhone` (string, optional)
  - `IbanNumber` (string, optional)
  - `VatNumber` (string, optional)
  - `PaymentPeriodDays` (int, optional)
  - `CreatedAtUtc` (DateTimeOffset), `CreatedBy` (Guid)
  - `UpdatedAtUtc` (DateTimeOffset, optional), `UpdatedBy` (Guid, optional)

- **VatSettings** (Entity — one per tenant)
  - `Id` (Guid)
  - `TenantId` (Guid)
  - `DefaultVatRate` (decimal) — defaults to 21%
  - `CreatedAtUtc` (DateTimeOffset), `CreatedBy` (Guid)
  - `UpdatedAtUtc` (DateTimeOffset, optional), `UpdatedBy` (Guid, optional)

**Business Rules:**
- Only the Owner can manage tenant settings
- Company information is used on invoices (name, address, VAT number, IBAN)
- Default VAT rate is applied to new services unless overridden per service

---

### 8. Onboarding & Subscriptions

Manages salon sign-up and subscription lifecycle. Stored in a separate **Website database** (not tenant-scoped).

**Entities:**

- **Subscription** (Aggregate Root)
  - `Id` (Guid)
  - `SalonName` (string)
  - `OwnerFirstName` (string)
  - `OwnerLastName` (string)
  - `Email` (string)
  - `PhoneNumber` (string, optional)
  - `Plan` (SubscriptionPlan)
  - `BillingCycle` (BillingCycle, optional)
  - `TrialEndsAtUtc` (DateTimeOffset, optional)
  - `CreatedAtUtc` (DateTimeOffset), `CreatedBy` (Guid, optional)
  - `ProvisionedAtUtc` (DateTimeOffset, optional), `ProvisionedBy` (Guid, optional)
  - `CancelledAtUtc` (DateTimeOffset, optional), `CancelledBy` (Guid, optional)
  - `CancellationReason` (string, optional)

**Derived Status (no status column — derived from timestamps):**
- **Pending**: `CreatedAtUtc` is set, no other timestamps
- **Active**: `ProvisionedAtUtc` is set, `CancelledAtUtc` is null
- **Trial**: `TrialEndsAtUtc` is set and in the future
- **Cancelled**: `CancelledAtUtc` is set

**Enums:**

- **SubscriptionPlan**: `Starter`, `Team`, `Salon`
- **BillingCycle**: `Monthly`, `Annual`

**Business Rules:**
- A subscription represents a salon's sign-up via the public website
- Provisioning creates the tenant (Keycloak realm, database, owner account)
- Only platform admins can provision, update plan, or cancel subscriptions

---

## Cross-Cutting: Identity & Multi-Tenancy

Identity and tenancy are **not** a separate bounded context but a cross-cutting concern handled at the infrastructure level.

- **Tenant**: represents a salon. All entities carry a `TenantId`.
- **User**: an authenticated identity linked to a `StaffMember` within a tenant.
- Auth and tenant resolution are handled via middleware (determined by ADR-007 and ADR-008).

---

## Entity Relationships

```
Tenant (cross-cutting)
  ├── StaffMember (1..N)
  │     └── WorkingHoursEntry (0..N)
  ├── Client (0..N)
  │     └── Recipe (0..N)
  │           ├── → Booking (N:1)
  │           ├── → StaffMember (N:1)
  │           └── RecipeProduct (0..N)
  ├── Service (1..N)
  │     └── ServiceCategory (0..N)
  ├── Booking (0..N)
  │     ├── → Client (N:1)
  │     ├── → StaffMember (N:1)
  │     └── BookingService (1..N)
  │           └── → Service (snapshot, not FK)
  ├── Invoice (0..N)
  │     ├── → Booking (1:1)
  │     ├── → Client (N:1)
  │     └── InvoiceLineItem (1..N)
  ├── Notification (0..N)
  │     └── → Booking (N:1)
  ├── EmailTemplate (0..5)
  ├── TenantSettings (1:1)
  └── VatSettings (1:1)

Subscription (separate database — not tenant-scoped)
  └── → Tenant (1:1, created on provisioning)
```

---

## Ubiquitous Language

| Term | Definition |
|------|-----------|
| **Tenant** | A single salon or barbershop location. All data is scoped to a tenant. |
| **Booking** | A scheduled visit by a client with a staff member, containing one or more services. Never called "appointment". |
| **BookingService** | A snapshot of a service attached to a booking (name, duration, price copied at creation time). |
| **Client** | A person who receives services at the salon. Never called "customer" or "patient". |
| **Staff Member** | A person who works at the salon and performs services. Never called "employee" or "provider". |
| **Service** | A specific offering from the salon catalog (e.g. "Men's Haircut", "Full Color"). |
| **Service Category** | A grouping of services (e.g. "Haircuts", "Coloring", "Treatments"). |
| **Invoice** | A billing document generated from a completed booking. |
| **Recipe** | A record of products and techniques used during a client's visit, linked to a specific booking. |
| **Working Hours** | The recurring weekly schedule defining when a staff member is available. |
| **Tenant Settings** | Per-tenant company information (name, address, IBAN, VAT number) used on invoices and communications. |
| **VAT Settings** | Per-tenant default VAT rate configuration. |
| **Subscription** | A salon's sign-up record, tracking plan, billing cycle, and provisioning status. Stored in the website database. |
| **Owner** | The staff role with full admin access to a tenant. One per tenant. |
| **Manager** | A staff role that can manage staff and schedules but not billing or tenant settings. |
| **No-Show** | A booking where the client did not arrive. Tracked as a booking status. |

---

## User Roles & Permissions

| Action | Owner | Manager | Staff Member |
|--------|:-----:|:-------:|:------------:|
| Manage tenant settings | Yes | No | No |
| Manage staff members | Yes | Yes | No |
| Manage service catalog | Yes | Yes | No |
| Manage clients | Yes | Yes | Yes |
| Create/edit bookings | Yes | Yes | Own only |
| View all bookings | Yes | Yes | No |
| View own bookings | Yes | Yes | Yes |
| Manage billing/invoices | Yes | No | No |
| Manage recipes | Yes | Yes | Yes |
| View reports/dashboard | Yes | Yes | No |

### Platform Admin

A separate role outside of tenant scope. Platform admins access the **Admin Portal** to manage subscriptions (provision, update plan, cancel). They authenticate via a dedicated Keycloak realm (`chairly-admin`).
