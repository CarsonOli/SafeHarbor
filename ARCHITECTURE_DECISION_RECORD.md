# ADR-0001: SafeHarbor Target Architecture and Delivery Standards

- **Status:** Accepted
- **Date:** 2026-04-06
- **Owners:** SafeHarbor team

## Context

SafeHarbor needs a clear, documented architecture baseline that aligns frontend and backend technologies, authentication/authorization strategy, sensitive-data handling, and team delivery practices. This ADR establishes the default implementation path for the current phase of the project.

## Decision

### 1) Frontend architecture

We will standardize the frontend on **React + TypeScript + Vite**.

- React provides a mature component model and ecosystem.
- TypeScript improves correctness and maintainability via static types.
- Vite gives fast local development and a simple build pipeline.

The app will use **route-based layout splitting** with at least two top-level route groups:

- **Public layout**: public-facing and low-privilege experiences.
- **Admin layout**: authenticated staff/admin experiences.

This split provides a clean boundary for navigation, UI composition, and role-aware route guarding.

### 2) Backend architecture

We will standardize the backend on **ASP.NET 10 Web API** with a layered structure:

- `API`: controllers/endpoints, request contracts, response shaping, middleware wiring.
- `Application`: use cases, orchestration, validation, DTO mapping, policy-level business workflows.
- `Domain`: core entities, value objects, domain invariants, domain services.
- `Infrastructure`: persistence, external systems, storage, integrations, concrete implementations.

Dependencies should flow inward (outer layers depend on inner abstractions), with Infrastructure implementing interfaces defined by Application/Domain where appropriate.

### 3) Database platform and cloud target

We choose **PostgreSQL** as the primary relational database platform.

#### Why PostgreSQL

- Strong relational consistency and transactional integrity for case management workflows.
- Rich indexing and query capabilities for reporting/search needs.
- Good support for semi-structured fields through JSON/JSONB when needed.
- Broad tooling and portability across local/dev/test/prod environments.

#### Considered alternative: Azure SQL

Azure SQL is a valid alternative for organizations that need deep Microsoft SQL Server compatibility. We are not selecting it as the default now, but we can revisit if future operational constraints require it.

#### Managed cloud target

- **Compute/API hosting:** Azure App Service.
- **Managed database:** Azure Database for PostgreSQL (Flexible Server).

This target minimizes operational overhead while preserving production-grade security, backup, and scaling options.

### 4) Authentication and authorization

We will use **Microsoft Entra ID** for staff and admin identity.

- Authentication: OpenID Connect/OAuth2 through Entra ID.
- Authorization: role-based policy enforcement.

Required application roles:

- `Admin`
- `SocialWorker`
- `Fundraising`
- `Viewer`

All Admin routes and backend endpoints must enforce explicit policy checks. Role checks must be deny-by-default.

### 5) Data sensitivity model and protections

Data must be classified and handled according to sensitivity.

#### Classification tiers

1. **PII (Personally Identifiable Information)**
   - Examples: names, personal contact details, addresses, identifiers.
2. **Child-sensitive data**
   - Examples: placement details, child welfare notes, incident/assessment records.
3. **Financial data**
   - Examples: donation records, payment references, grant allocations, ledger-like transaction details.

#### Protection requirements

- **At rest:** encryption enabled for database/storage and backups.
- **In transit:** TLS required for all service-to-service and user-to-service traffic.
- **Access control:** least privilege + role-based authorization at endpoint and (where appropriate) row/query scope.
- **Data minimization:** only collect/store required fields; avoid sensitive data duplication.
- **Auditability:** log access to sensitive operations and admin actions with immutable audit trails.
- **Secrets management:** no secrets in source control; use managed secret store and environment-based configuration.
- **Retention and purge:** define retention windows and secure deletion strategy by data class.

## Consequences

### Positive

- Team alignment on a single, documented architecture direction.
- Faster onboarding and more predictable code reviews.
- Clear identity/security baseline for staff-facing workflows.
- Better risk management for sensitive case and financial data.

### Tradeoffs

- Layered architecture introduces extra project structure and initial setup work.
- Strict role/policy and classification controls add implementation overhead.
- Choosing one default DB platform may require migration work if requirements materially change later.

## Implementation notes

- This ADR is the baseline unless superseded by a later ADR.
- Any exceptions must be documented in a follow-up ADR with rationale and migration impact.
