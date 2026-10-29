# Manufacturing ERP Database Design

## 1. Status and design rules

This document describes the target relational design and records the implemented database foundation. Only the initial platform, master-data, product, and BOM scope below has been modeled in EF Core; the remaining modules are still conceptual.

- PostgreSQL is the recommended transactional system of record.
- Normalize master and transaction data; retain immutable source documents and append-only postings.
- Every business record is scoped to a company; add site, warehouse, or organizational scope where the owning domain requires it.
- Use generated UUID primary keys for relationships and unique human-readable document numbers scoped by company and document type.
- Apply restrictive deletion to referenced masters and posted business records. Inactivate/archive rather than deleting history.
- Use database constraints for critical uniqueness, references, allowed values, and positive/nonnegative quantity rules.
- Use `numeric`, not floating-point, for quantities, conversion, prices, cost, and money. The foundation uses `numeric(20,6)` for BOM quantities, `numeric(20,10)` for conversion factors, and `numeric(9,6)` for BOM waste percentages. Monetary precision and rounding remain a finance/domain decision.
- Version business structures and snapshot the exact approved revisions used by planning and work orders.
- JSON is limited to extensible metadata/event payloads with schema/version; core quantities, statuses, joins, and reporting attributes remain typed columns.

### 1.1 Implemented foundation status

The initial EF Core/Npgsql migration is `20261029044859_InitialDatabaseFoundation` in `src/backend/Erp.Infrastructure/Persistence/Migrations`. The follow-up `AuthenticationAndRbac` migration adds local credential/session-revocation fields to `UserAccounts` and the security audit stream. Together, the migrations create these tables:

- Platform and access catalog: `Companies`, `UserAccounts`, `Roles`, `Permissions`, `UserCompanyRoles`, `RolePermissions`, `AuditEvents`, `SecurityAuditEvents`.
- Master data: `Customers`, `Suppliers`, `UnitsOfMeasure`, `UomConversions`, `ItemCategories`, `Items`, `Warehouses`, `WarehouseLocations`.
- Product engineering: `Products`, `ProductVariants`, `ProductVariantAttributes`, `Boms`, `BomVersions`, `BomComponents`.

The implemented relationships are: a company owns its customers, suppliers, item categories/items, units/conversions, warehouses/locations, roles, products, and BOM records; user accounts receive company roles through `UserCompanyRoles`, and roles receive permissions through `RolePermissions`. Items reference a same-company category and stock/purchase UOMs; conversions link two same-company UOMs. Warehouses own locations, whose optional parent must be in the same warehouse. Products own variants, each variant links to a same-company stock item, and variant attributes belong to a variant. A product can have BOMs, each BOM has versioned BOM records, and each BOM component references a same-company item and UOM. Audit events reference their company and optional user, while their generic entity type/ID is retained as a historical snapshot.

Implemented database protections include company-scoped composite foreign keys, restrictive deletion for referenced records, unique company-scoped codes, unique role/permission assignments and product/BOM relationships, nonblank codes, allowed tracking/BOM statuses, date-range checks, positive conversion/item/BOM quantities, BOM approval-state consistency, and bounded UOM precision and waste percentages. The tests use an in-memory SQLite database for relational constraints and check the EF model's quantity precision; PostgreSQL integration tests are still required.

The migration has been generated and its idempotent PostgreSQL SQL script reviewed, but it has not been applied to a database. Applying it requires a reachable PostgreSQL instance and a valid `ConnectionStrings__Erp`; the design-time placeholder used to generate artifacts is not a usable database credential. No destructive migration or data operation has been run.

`UserAccounts` stores an external identity subject; this schema does not implement login, passwords, sessions, or authentication. `Permissions` is a global catalog, while role assignments are company-scoped. `AuditEvents` retains actor and entity snapshots and a JSONB details payload; its generic entity reference intentionally does not claim a foreign key to every future business table. Finance tables and monetary columns are not part of this foundation.

The authentication migration adds nullable `NormalizedEmail` and `PasswordHash` for accounts that do not use local passwords, a unique filtered index on normalized email, a security stamp used to revoke cookies, a failed-password counter, and a lockout end time. `SecurityAuditEvents` supports system-wide events with optional company, actor, and target-user references so failed sign-in attempts can be recorded without disclosing whether an account exists. Passwords and session tokens are never written to audit details. Password hashing/session handling uses ASP.NET Core identity primitives in the API; this is not a custom password format.

## 2. Relationship overview

```text
Company -> Sites -> Warehouses -> Bins
Company -> Users <-> Roles <-> Permissions
Customer -> Buyer PO -> Sales Order -> Lines / Delivery Schedules -> Firm Demand
Product/Style -> Variants
Product/Style -> BOM -> BOM Versions -> Components (Items)
Product/Style -> Routing -> Routing Versions -> Operations
Demand + BOM/Routing snapshots -> MRP Plan -> Material Requirements / Shortages
Shortage -> Purchase Requisition -> PO -> Receipts -> Inspection -> Inventory Ledger
Demand -> Work Order -> Material Requirements -> Job Cards -> Confirmations -> Quality
Work Order -> Material Issue/Return/Consumption -> Inventory Movements
Work Order -> Job Work Order -> Supplier Custody -> Return/Inspection/Reconciliation
Accepted production -> Finished Goods Receipt -> Packing/Cartons -> Dispatch -> Delivery
All state-changing aggregates -> Approval/Audit/Outbox; external messages -> Integration Log
```

## 3. Proposed entity groups

### 3.1 Platform and reference masters

- `Company`, `Site`, optional `Department` and `ProductionLocation`.
- `UserAccount` keyed to stable external subject or selected local identity; `Role`, `Permission`, `UserCompanyRole`, `RolePermission`.
- `ApprovalPolicy`, `ApprovalRequest`, `ApprovalDecision` with target type/id and target revision. Keep a direct FK to each aggregate where practical; a generic target reference alone cannot enforce referential integrity.
- `AuditEvent` (actor, scope, action, entity reference, timestamp, correlation ID, redacted details).
- `DocumentSequence` with company/type/period scope and concurrency-safe allocation.
- `UnitOfMeasure`, `UomConversion` with dimension, direction, factor, effective interval, rounding policy.
- `Customer`, customer address/contact/commercial terms; `Supplier`, supplier address/contact/lead-time/terms.
- `ItemCategory`, `Item`, item-supplier reference, tracking policy, purchase-to-stock conversion, reorder policy.
- `Warehouse`, `WarehouseBin`, `Machine`, `ProductionLine`, `Shift`, `OperationCatalog`.

### 3.2 Product engineering

- `Product` / `Style` with company, industry-pack discriminator/reference, code, lifecycle.
- `ProductVariant` with product ID and normalized attribute values; enforce company/product-scoped variant identity. Apparel size/color and footwear size/width are pack-level dimensions, not universal mandatory columns.
- `AttributeDefinition`, `AttributeValue`, and `VariantAttribute` only if flexible governed characteristics are required. Frequently queried/stock-defining dimensions should be modeled explicitly or indexed projections with integrity checks.
- `Bom`, `BomVersion`, `BomComponent` with component item, quantity/UOM, scrap/waste policy, optional/alternative group, sequence, and effective dates.
- `Routing`, `RoutingVersion`, `RoutingOperation` with operation sequence, work center/line capability, standard time, subcontract flag, and effective dates.
- Approved versions are immutable. Changes create a new version; record who approved it and when.

### 3.3 Sales and demand

- `BuyerPurchaseOrder` (buyer reference, date, customer, currency, status) and its lines, or an equivalent explicit reference on a Sales Order; choose one canonical buyer-PO model during discovery.
- `SalesOrder`, `SalesOrderLine`, `SalesOrderDeliverySchedule`, `SalesOrderRevision`, `SalesOrderHistory`.
- Sales lines reference customer/product/variant and retain code/name/UOM/price snapshots where necessary for historical display.
- `Demand` identifies `MTO`, `MTS`, or another approved source, source aggregate/line, variant/item, quantity/UOM, due date, and lifecycle. Use unique source identity to prevent duplicate demand generation on retry.
- `DemandAllocation` relates demand to one or more work orders, shipment allocations, or replenishment plan quantities; reconcile allocated vs unallocated demand.

### 3.4 Planning and procurement

- `MrpRun` stores run ID, planning horizon/as-of time, policy/version, input watermark, and status.
- `MrpRequirement` stores source demand, item/variant, BOM version/snapshot, gross, eligible on-hand, reserved, scheduled supply by due date, net shortage, and calculation explanation.
- `PurchaseRequisition`, `PurchaseRequisitionLine`, optional RFQ/quotation/comparison, `PurchaseOrder`, `PurchaseOrderLine`.
- PO lines optionally link to originating requisition lines and persist vendor, ordered quantity/UOM, conversion, price/currency, and promised date.
- `GoodsReceipt`, `GoodsReceiptLine` link to PO lines and record actual receipt and batch/lot; `ReceiptInspection` and disposition are separately recorded.
- Receipt quantity, accepted, quarantined, rejected, returned, and outstanding quantities must reconcile by line.

### 3.5 Inventory and valuation

- `InventoryTransaction` (business posting header) and append-only `InventoryMovement` (item, company/site, warehouse/bin, lot, status, signed quantity/UOM, source document/line, actor/time, idempotency key).
- `InventoryBalance` may be a rebuildable projection keyed by company/site/item/location/lot/stock status. It is not directly editable from UI.
- `StockReservation` relates demand/work order to location/lot/status and quantity. Reservation reduces availability, not physical on-hand.
- Transfers use `StockTransfer`/lines plus dispatch and receipt events; represent in-transit stock separately where receipt is a separate action.
- If FIFO is approved, `CostLayer` references receipt/source movement; `CostLayerAllocation` links an issue to consumed layer amounts. Preserve reversals and returns.
- Supplier-held company-owned stock belongs to a distinct `SupplierCustodyMovement`/balance or an explicitly segregated status/location dimension; never treat it as normal warehouse availability.
- Record valuation policy and currency/rounding on cost postings; physical quantities and financial values must not be conflated.

### 3.6 Work execution and quality

- `WorkOrder`, `WorkOrderRevision`, `WorkOrderDemandAllocation`, `WorkOrderMaterialRequirement`, `WorkOrderOperationSnapshot`.
- Work order retains exact BOM/routing version and any resolved material/operation snapshot used for release.
- `ProductionPlan`, `ScheduleRevision`, capacity/line/shift allocation.
- `JobCard`, `JobCardAssignment`, `JobCardTransition`, `ProductionConfirmation` with operation, line/machine/shift/team, partial quantities, start/end, downtime, idempotency key.
- Separate `MaterialIssue`, `MaterialReturn`, `MaterialConsumption`, `ScrapRecord`, `ReworkOrder`; each posted quantity links to movements and relevant work order/job card.
- `QualityPlan`, `Inspection`, `InspectionSample`, `Defect`, `Disposition` link to exact receipt/lot/job card/confirmation/finished-goods subject. Record inspected, accepted, held, rework, and rejected quantities explicitly.
- Model actual output reconciliation based on configured counting policy; do not infer accepted stock solely from a confirmation.

### 3.7 Job work, maintenance, workforce, fulfillment, finance

- `JobWorkOrder`, `JobWorkDispatch`, `JobWorkReceipt`, `SubcontractorCustodyMovement`, `JobWorkReconciliation`, `JobWorkCharge`.
- `MachineMaintenancePlan`, `MaintenanceRequest`, `MaintenanceWorkOrder`, `DowntimeEvent`, spare-part issue reference, cause/action/cost/approval.
- `Employee`, `Skill`, `EmployeeSkill`, `ShiftAssignment`, `Attendance`, `Leave`, `Team` and team membership. Segregate sensitive HR columns/schema and access paths.
- `FinishedGoodsReceipt`, `PackingPlan`, `Carton`, `PackedLine`, `DispatchOrder`, `DispatchLine`, `DeliveryConfirmation`; reference Sales Order line, variant, lot, and eligible inventory movement.
- `CostPolicy`, `CostEntry`, `WorkOrderCostSnapshot`, `CostVariance`, and external accounting references. Keep finance integration distinct from a full ledger until scoped.

### 3.8 Cross-cutting and integration

- `Notification`, `NotificationPreference`, `EmailDeliveryAttempt`.
- `Attachment` stores safe metadata and an opaque storage key, not arbitrary public paths.
- `OutboxMessage` in the same transaction as the business change; `IntegrationEndpoint`, `IntegrationMessage`, `IntegrationAttempt`, `IdempotencyRecord`.
- `ReportDefinition` / `ExportJob` if persistent or asynchronous exports are needed.

## 4. Keys, constraints, and indexes

The foundation migration implements the following baseline constraints. Constraints listed as target expectations remain to be added with their owning modules:

- Implemented: unique `(CompanyId, Code)` for company-scoped masters and products, and global uniqueness for identity subjects and permission codes. Future documents still need unique `(CompanyId, DocumentType, Number)` constraints.
- Unique source identity for generated demand and `(CompanyId, Consumer, IdempotencyKey)` for commands/postings.
- Implemented: unique company/role code, user-company-role and role-permission assignments, product-variant codes and item links, `(CompanyId, BomId, VersionNumber)`, and BOM component line numbers. Future sales, routing, and job-card sequence constraints belong to those modules.
- Implemented: composite foreign keys enforce same-company item/category/UOM, product/variant, BOM, and warehouse/location relationships.
- Implemented: checks cover nonblank codes, UOM decimal places, tracking/BOM status, BOM approval metadata, date intervals, and positive item/UOM/BOM quantities. Financial, inventory-posting, and disposition checks remain future work.
- Common indexes: company/status/date document queries; customer/order/due-date; item/location/lot/status movement and balance lookups; PO-line receipt matching; work order/operation/status; outbox ready/attempt time; source reference/idempotency key.
- Avoid duplicate indexes created by redundant EF relationships; inspect query plans for operational reports.

## 5. Transaction boundaries and consistency

1. A command changes one owning aggregate and writes its state history, audit, and outbox row atomically.
2. Cross-module synchronous work uses a defined application service/port and one database transaction only when the business invariant truly requires atomicity. Otherwise publish an outbox event and make the consumer idempotent.
3. Inventory posting locks or version-checks all affected balances/layers deterministically, validates eligible stock, appends movement, updates projection/valuation, and commits as one unit.
4. Use PostgreSQL integration tests for isolation, constraints, migrations, numeric behavior, and simultaneous issues. Do not infer PostgreSQL correctness from SQLite tests.
5. Posted records are immutable. Reversal references the original, carries actor/reason, and is itself idempotent.
6. Rebuild projections from the ledger and reconcile counts/cost layers; never silently balance discrepancies.
7. Work order release snapshots the approved engineering revision and planning inputs. Later revisions do not rewrite released work.
8. Receipt quantity, inspection disposition, work order output, operation transfers, supplier custody, packed quantity, and dispatch quantities are reconciled explicitly.

## 6. Data lifecycle, privacy, and migration policy

- Inactivate masters when referenced; archive only under a documented retention policy.
- Retain approvals, audit, posted movements, source snapshots, and reversals for policy-defined periods.
- Apply additive, reviewed migrations with a rollback/forward-recovery plan; large data backfills need staged deployment.
- Encrypt sensitive data and attachment access according to classification; logs must not include passwords, tokens, or unnecessary employee data.
- Define backup frequency, point-in-time recovery, restore testing, and RPO/RTO before production.
