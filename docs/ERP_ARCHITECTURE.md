# Manufacturing ERP Architecture

## 1. Architecture goals

Build a locally developable, deployable manufacturing ERP as a **modular monolith**: one deployable application with cohesive business modules, explicit interfaces, and transaction boundaries. Start with a garment/apparel industry pack while keeping shared manufacturing concepts reusable for footwear and other industries.

This is a target architecture, not a description of implemented software.

## 2. Recommended technology baseline

| Area | Recommendation | Rationale |
|---|---|---|
| Backend | ASP.NET Core Web API on .NET 10 LTS | Typed contracts, mature authentication/authorization, background processing, and transaction-capable application services |
| Database | PostgreSQL | Relational constraints, strong transactions, JSON only for bounded metadata, and suitable query/index capabilities |
| Persistence | EF Core with Npgsql migrations | Explicit schema evolution and familiar .NET integration |
| Frontend | React + TypeScript + Vite | Productive workflow UI, typed contracts, fast local development |
| API contract | Versioned REST + OpenAPI | Stable browser/MES integration boundary and generated clients where useful |
| Tests | xUnit; PostgreSQL integration tests; frontend component tests; browser E2E for critical flows | Validate rules at domain, persistence, browser, and production-like layers |
| Local development | Frontend, API, and PostgreSQL on the developer's host; provider services separately configurable | Clear localhost topology; no dependency on an outsourced ERP API |

Pin supported versions in manifests and lockfiles when the project is created. Do not install tools until a bounded implementation slice needs them. Use a provider-neutral OIDC integration for deployed identity; decide how local sign-in is provided before implementing authentication. Do not add a production-insecure development bypass.

## 3. Runtime topology and request flow

```text
Browser
  -> React feature screens / typed API client
  -> same-origin development proxy (local Vite -> local ASP.NET Core API)
  -> authentication/session, authorization, API versioning, validation
  -> module application command/query handlers
  -> module-owned domain rules and transaction boundary
  -> EF Core / PostgreSQL
  -> outbox worker -> email, MES, document provider, other external adapters
```

In local development, the API, frontend, and database are separately addressable local processes. For example, reserve one loopback port per service in configuration and document health endpoints. Do not point the proxy at a remote API by default. OIDC is an identity service, not the ERP backend, and may be configured independently.

## 4. Backend layering and dependency rules

Organize the backend by business module and keep these responsibilities explicit:

1. **Domain:** entities/aggregates, value objects, invariants, lifecycle transitions, and domain events. No EF, HTTP, identity-provider, or UI dependency.
2. **Application:** use cases/commands/queries, authorization-relevant checks, transaction coordination, idempotency, and interfaces for persistence/time/messaging. References domain, not infrastructure.
3. **Infrastructure:** EF Core mappings/migrations, repositories/query adapters, outbox, email/MES/document adapters, and implementation of application ports.
4. **API host:** composition root, authentication/session middleware, authorization policies, request/response contracts, API versioning, error mapping, and dependency injection. It calls application use cases, not module tables directly.
5. **Frontend:** route/layout shell, feature modules, typed API client, client-side presentation validation, and accessible workflow components. It never contains authoritative business rules or database access.

Dependency direction:

```text
API -> Application -> Domain
Infrastructure -> Application + Domain
Composition root wires API + Infrastructure + module Application
Frontend -> published API contracts only
```

Avoid one generic repository per table, direct cross-module database writes, a universal workflow engine before real use cases need one, and premature microservices. Extract a service only when independent scaling, ownership, or deployment justifies distributed-system complexity.

## 5. Business module boundaries and relationships

Each module owns its schema namespace/tables, commands, lifecycle, authorization actions, and tests. Cross-module references use stable IDs and application contracts; synchronous commands are used when one business transaction must be atomic, while outbox events are used for asynchronous propagation.

| Module | Owns | Depends on / publishes |
|---|---|---|
| Platform & identity | Company/site scope, users, roles, permissions, policy, approvals, audit, configuration, numbering | Shared scope and identity contracts for all modules |
| Master data | Customer, supplier, item, UOM, warehouse, bin, machine, shift, employee reference, operation catalogs | Platform scope; publishes master changes |
| Product engineering | Product/style, variant attributes, BOM/routing versions, engineering approval | Master data; publishes approved effective versions |
| Sales & demand | Buyer PO, Sales Order, order amendments/schedules, firm MTO demand | Customer/product/variant; publishes confirmed demand |
| Planning & MRP | Demand snapshots, time-phased supply/demand calculation, capacity schedule, shortage proposals | Sales demand, approved BOM/routing, inventory availability, open procurement/work orders |
| Procurement | Requisition, RFQ/quotation, PO, supplier delivery commitments, receipt source | Supplier, approved shortage; receipt command to inventory/quality |
| Inventory | Append-only ledger, balances/projections, reservations, lots/status, transfers, FIFO layers if approved | Master data; accepts source-linked procurement/production/dispatch postings |
| Manufacturing | Work orders, material requirements snapshot, job cards, operations, confirmations, WIP, rework/scrap records | Demand, engineering snapshots, planning, inventory issue/return, quality disposition |
| Quality | Inspection plans/results, defects, holds, disposition, corrective action | Receipt, production, job work, finished goods; releases or restricts stock through authorized commands |
| Job work | Outsourced operation orders, supplier custody, dispatch/receipt reconciliation, charges | Manufacturing routing, supplier, inventory custody ledger, quality |
| Maintenance | Equipment maintenance plans/requests/work orders, downtime, spare requirements | Master data, inventory, production schedule |
| Workforce / HR | Employee details, skills, shifts, attendance, training, team assignments | Platform privacy controls; production references team/shift, not unrestricted HR data |
| Fulfillment | Finished-goods receipt request, packing/cartons, picking, shipment, delivery confirmation | Quality-released inventory, Sales Order, Manufacturing genealogy |
| Finance & costing | Cost policies, source cost capture, work/order cost, variance, accounting integration boundary | Inventory valuation, purchasing, job work, labor/time, production; does not rewrite their postings |
| Notifications & documents | Notification preferences/delivery attempts, attachment metadata/access policy | Outbox events and module authorization; does not own business status |
| MES/integration | External endpoint configs, inbound/outbound messages, idempotency, replay/reconciliation | Versioned module contracts; ERP commands remain posting authority |
| Reporting | Read models/KPIs, filters, export jobs, report definitions | Authorized module query APIs/events; read-only against transactional postings |

### Shared kernel (keep deliberately small)

Use shared primitives only for concepts whose meaning is genuinely common: company/site IDs, actor/correlation/idempotency metadata, quantity plus UOM identity, money plus currency, document references, and event/outbox envelopes. Do not put product, order, stock, approval, or industry behavior into a catch-all shared library.

## 6. Industry extension model

Core manufacturing represents item, product/variant attributes, BOM, routing/operation, demand, work order, inventory status/movement, quality disposition, and fulfillment through general concepts.

An industry pack supplies governed catalogs, validations, optional module capabilities, workflows, reports, and UI sections:

- **Garment/apparel pack:** style, size/color matrix, season, fabric/trim/packaging consumption, cutting plan/bundles, line/operation tracking, inline/end-line inspection, carton assortment.
- **Footwear pack (future):** last/model, size/width matrix, sole/upper/component structures, lasting/finishing operations, pair/box/carton reconciliation.
- **Other industries:** add only after discovery, with requirements and acceptance tests for the industry.

Prefer explicit, versioned extension interfaces and metadata-backed attributes for bounded differences. Promote an attribute or behavior to core only when at least two real industry use cases share its semantics. Do not use free-form JSON as a substitute for core relational data or permit extension code to bypass permissions, inventory posting, or audit.

## 7. Transaction and integration patterns

- A module command validates actor/scope/state, checks idempotency, applies domain rules, persists its aggregate, audit record, and outbox event in one database transaction.
- For a stock posting, validate reservation/eligibility and concurrency under a database transaction; append source-linked movement; update/rebuild projection and valuation allocation; record audit/outbox; commit atomically.
- Prefer database constraints and unique idempotency/document keys as final guards. Use optimistic concurrency for editable aggregates and provider-appropriate row locking/serializable logic for contested stock.
- No email/MES call inside the transaction. An outbox worker retries delivery with bounded backoff and stable IDs; failure is visible and reconcilable.
- Integration consumers validate schema/version, company scope, event identity, and quantity rules before invoking a normal application command.
- Use cancellation/reversal/adjustment records for posted transactions; never silently overwrite the ledger.

## 8. Security, operations, and observability

- The initial local API uses ASP.NET Core cookie authentication with the framework `PasswordHasher<UserAccount>`; passwords are stored only as versioned password hashes. Browser session cookies are `HttpOnly`, `Secure`, `SameSite=Strict`, host-only cookies with a 30-minute sliding expiry. Require HTTPS for the API.
- Cookie-authenticated mutations require an ASP.NET Core antiforgery request token in `X-ERP-CSRF`; obtain one from `GET /api/v1/auth/csrf`. Login is rate-limited by client IP and locks accounts after repeated failed password attempts.
- The API fallback policy requires authentication. Administrative endpoints require database-backed, company-scoped permission codes (for example `security.users.create`); authorization checks active users, memberships, roles, and current permission assignments on the server. Client-side controls are never an authorization boundary.
- A one-time bootstrap endpoint is mapped only when `Security__BootstrapToken` is configured with at least 32 characters. It requires the token in `X-ERP-Bootstrap-Token`, refuses to run after an account exists, creates the initial company roles/permission catalog, and provisions the first Administration account. Do not use a committed or reusable default bootstrap token.
- The bootstrap creates Sales, Purchase, Stores, Production, Quality, Maintenance, HR, Dispatch, Finance, and Administration roles. Administration starts with all catalog permissions; module roles receive only their configured module and action permissions. Existing role assignments and permission edits are audited.
- Enforce policy at the API/application boundary and company/site scope in every query and write. UI visibility is not authorization.
- Store secrets outside source control; use a local secret store/environment for development and a managed secret store for deployed environments.
- Audit login success/failure, logout, bootstrap, user/role/permission changes, approvals, posting, reversal, sensitive exports, and HR access without storing credentials, password hashes, tokens, or unnecessary personal data.
- The current local password sign-in is an initial deployment mode. Before internet-facing production, complete MFA/recovery and credential lifecycle design, rate-limit/proxy review, Data Protection key-ring deployment, security testing, and operational secret rotation; use OIDC when the organization selects an identity provider.
- Structured logs with correlation ID; redact secrets; expose non-sensitive liveness/readiness separately.
- Apply schema migrations as a reviewed deployment step; test backup/restore and set RPO/RTO before production.
- Publish queue age, failed outbox count, integration failures, request latency/error rates, and database health.

## 9. Testing strategy

See [SECURITY_AUTHENTICATION.md](./SECURITY_AUTHENTICATION.md) for the local bootstrap, browser-session, antiforgery, and user/role API contracts.

- Domain unit tests for transitions, quantities, precision, and policy decisions.
- Application tests for authorization, scope, idempotency, and transaction behavior.
- PostgreSQL integration tests for constraints, JSON mappings, isolation, row locking, migration application, and competing stock transactions. SQLite tests are useful but do not replace PostgreSQL tests.
- Frontend tests for forms, status transitions, accessibility, error/loading states, and API contract mapping.
- Browser E2E tests for order-to-dispatch critical paths once those workflows exist.
- Contract/replay tests for email/MES outbox consumers and versioned API payloads.

## 10. Major architecture decisions for review

1. Approve .NET 10 / PostgreSQL / EF Core / React TypeScript as the initial baseline, or specify an alternative.
2. Select local PostgreSQL installation strategy and identify who owns backup/restore.
3. Select OIDC provider and local development authentication strategy.
4. Confirm the first garment workflow and first product/size/color model.
5. Decide company/site tenancy, approval segregation, inventory status policy, and whether FIFO is required in the first inventory release.
6. Confirm local-only initial deployment versus a later hybrid/central deployment target.
