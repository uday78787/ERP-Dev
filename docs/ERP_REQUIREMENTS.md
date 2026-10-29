# Manufacturing ERP Requirements

## 1. Purpose and scope

Design a browser-based ERP for a medium-sized manufacturer. The first industry pack is garment and apparel manufacturing. The core must also support future industry packs without embedding apparel assumptions into shared transactions.

This document states product requirements and target behavior. It does not claim that any application, database, integration, or control is implemented.

## 2. Product principles

1. A business transaction is a controlled lifecycle, not an unrestricted CRUD record.
2. Every operational quantity and value must be traceable to its source, actor, time, unit, status, and company/site scope.
3. Posted inventory, quality, production, and financial events are append-only; corrections use linked reversals or adjustments.
4. A module owns its invariants and data. Other modules use published application commands and queries rather than editing its tables.
5. Authentication, authorization, approval, validation, and company scope are enforced server-side.
6. The database is the transactional source of truth. The UI never directly sets balances or authoritative status.
7. MTO demand from confirmed customer orders and MTS demand from an approved forecast/replenishment policy remain distinguishable.
8. External email and MES outages must not corrupt or block committed ERP transactions.
9. Industry-specific behavior is expressed through configured master data and bounded industry modules.
10. Demo/seed data is only for local development and tests; operational workflows use persisted records.

## 3. Users and access boundaries

Expected roles include sales/customer service, procurement, warehouse, planner, production supervisor, operator/team lead, quality inspector, subcontracting coordinator, maintenance technician, HR, finance/cost accountant, dispatch, auditor, and system administrator. One person may hold multiple roles subject to approval segregation rules.

Permissions are defined by action (read, create, submit, approve, post, reverse, export, administer) and scoped by company, site, warehouse, department, or assigned records as appropriate. HR personal data has a separate access policy. A requester must not approve their own transaction where policy requires maker-checker control. All privileged decisions are auditable.

## 4. Functional requirements by capability

### 4.1 Platform, identity, and administration

- Sign users in and out using a selected, standards-based identity strategy; use secure server-side sessions/cookies for the browser.
- Manage users, roles, permissions, company/site scope, configuration, approval policies, document numbering, and reference catalogs.
- Protect mutating browser requests against CSRF; use secure transport, safe redirect validation, and configurable session expiry.
- Provide audit history, health/readiness endpoints, notification preferences, attachment metadata, and integration status.

### 4.2 Customer, sales, and demand

- Maintain customer/buyer contacts, addresses, commercial terms, and status.
- Record buyer purchase orders and internal Sales Orders with style/product, variant, size/color breakdown where applicable, quantity, UOM, price/currency, and requested delivery schedule.
- Validate active masters and allowed dates/quantities; support draft, submit, approve/reject, confirm, hold/cancel, and controlled amendment lifecycles.
- Preserve revisions, approvals, audit history, and creator/approver identities.
- Confirmation creates a traceable demand source only when the demand module is available; it must not imply material availability or production completion.

### 4.3 Product, item, BOM, and routing

- Maintain unique controlled item codes, category, stock/purchase UOM and conversions, tracking/lot rules, status, and applicable sourcing/storage policies.
- Represent sellable products/styles and stockable variants. Apparel supports size/color/season/style dimensions as governed master data.
- Maintain multi-level BOMs and routing versions with components, standard consumption, waste, alternatives, operations, effective dates, approvals, and revision history.
- Freeze the BOM/routing version or explicit snapshot selected for a released work order.

### 4.4 Planning, procurement, and inventory

- Calculate order-/demand-wise gross and net material requirements from selected approved product structures, eligible on-hand stock, reservations, and time-phased expected receipts.
- Show supply assumptions, shortages, availability dates, and proposed purchase requirements. MRP suggests; authorized users approve requisitions and POs.
- Support supplier master, requisition and approval, optional RFQ/quotation comparison, PO issue, partial receipt, inspection, acceptance/rejection, and supplier return.
- Maintain a source-linked inventory ledger for receipts, inspection disposition, reservation, issue, return, transfer, count, adjustment, WIP, and finished goods.
- Distinguish on-hand, eligible, reserved, allocated, in-transit, quarantine, rejected, WIP, and subcontractor custody. Prevent unauthorized negative eligible stock.
- Support lot/batch and location/bin traceability and configurable valuation policy; FIFO is a candidate policy to validate with finance before implementing valuation.

### 4.5 Work execution, production, and quality

- Link work orders to demand, product/variant, approved BOM/routing, site, dates, quantity, and planning revisions.
- Define explicit approval, release, hold, resume, partial completion, cancellation, and closure transitions with preconditions and history.
- Generate job cards by operation; assign line, shift, machine, operator/team, target, and schedule where applicable.
- Post material issue, return, actual consumption, scrap, transfer, and shortage as separate linked transactions.
- Capture partial production confirmations with good, rejected, rework, scrap, downtime, time, shift, line, machine, and operator/team data.
- Keep reported production, inspected output, accepted output, rework, and finished-goods receipt distinct.
- Inspect incoming, in-process/inline, end-line, and final output using configurable plans/samples, defect codes, disposition, hold, rework, or rejection.
- Support outsourced processes (e.g., embroidery, print, washing) with supplier custody, dispatch/receipt, reconciliation, charges, and quality disposition.

### 4.6 Maintenance, workforce, fulfillment, and finance

- Maintain equipment, preventive schedules, breakdown/maintenance work, technician assignment, spares, cost, root cause, and downtime links.
- Maintain employees, skills, shifts, attendance, leave, overtime, and team assignments with privacy controls. Payroll/statutory processing is a separately scoped capability.
- Receive quality-released finished goods; pack by order and variant into traceable cartons; plan, pick, authorize, partially dispatch, and confirm delivery.
- Prevent dispatch exceeding eligible packed stock or remaining authorized order quantity.
- Capture material, labor, machine, subcontracting, scrap, and variance inputs for manufacturing cost. Integrate with a separate accounting/tax capability; do not imply a complete general ledger or tax compliance before delivery and validation.

### 4.7 Notifications, MES, reports, and dashboards

- Provide in-app notifications and configurable email delivery for approvals, shortages, delays, quality holds, maintenance, job work, and dispatch.
- Persist delivery attempts, errors, retries, and deduplication identifiers; do not expose credentials in logs.
- Provide a versioned, secured MES integration boundary for master data, released work, production counts, machine state/downtime, and quality results.
- Use idempotency keys, validation, integration logs, replay controls, and reconciliation; the ERP remains authoritative for ERP postings.
- Provide searchable, sortable, paginated operational views and authorized exports/printable reports.
- Define every KPI, source, filters, and time semantics. Dashboards must derive from persisted records, not hardcoded samples.

## 5. Cross-cutting quality requirements

Initial measurable targets to validate during discovery:

- **Integrity:** all posted ledger records reconcile to their source documents; no unapproved cross-company references.
- **Concurrency:** demonstrate database-backed tests that two concurrent stock issues cannot consume the same eligible quantity.
- **Idempotency:** retrying the same identified command creates no duplicate posting.
- **Performance:** target p95 below 2 seconds for common paginated reads and below 5 seconds for ordinary commands at the agreed pilot load; large reports may be asynchronous.
- **Availability/recovery:** set production availability, RPO, RTO, retention, and backup targets with operations before deployment. Exercise restore before production use.
- **Security:** least privilege, encrypted transport, managed secrets, audit for privileged actions, safe attachment access, and security testing before release.
- **Usability:** responsive screens optimized for office and shop-floor devices, clear validation, accessible keyboard/focus behavior, and explicit status/quantity labels.
- **Portability:** support local development and a separately configured deployment without embedding host-specific assumptions or secrets in source.

## 6. Exclusions until explicitly approved

Do not claim full accounting/ERP tax compliance, payroll/statutory compliance, automatic purchasing, real-time MES control, offline plant execution, or support for every future industry without scoped requirements, implementation, and acceptance tests.

## 7. Decisions required before first application slice

Confirm the initial factory/company/site model; login provider and local-development identity approach; database installation/ownership; shoe/apparel product variants and UOM policy; first user roles and approval limits; stock/negative-stock and valuation policy; deployment target; and pilot users/load. Prefer explicit decisions and reversible defaults over hidden assumptions.
