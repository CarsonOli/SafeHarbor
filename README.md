# SafeHarbor

This is Group 1-15's repo for Intex II.

## Architecture baseline

The project follows the architecture defined in `ARCHITECTURE_DECISION_RECORD.md` in the repository root:

- **Frontend:** React + TypeScript + Vite, with route-based layout split for Public vs Admin.
- **Backend:** ASP.NET 10 Web API using layered structure: `API`, `Application`, `Domain`, `Infrastructure`.
- **Database:** PostgreSQL as default relational datastore, targeting managed cloud deployment.
- **Cloud target:** Azure App Service + Azure Database for PostgreSQL (Flexible Server).
- **Identity:** Microsoft Entra ID for staff/admin authn with role-based policies.
- **Primary roles:** `Admin`, `SocialWorker`, `Fundraising`, `Viewer`.

## Authentication role mapping contract

SafeHarbor persists local-auth roles in `lighthouse.users.role` using lowercase DB values, then maps them to app authorization roles used by JWT claims, API policies, and frontend route guards.

| Database role (`lighthouse.users.role`) | App/API role (JWT + authorization) |
|---|---|
| `admin` | `Admin` |
| `staff` | `SocialWorker` |
| `user` | `Donor` |

- Treat this as a contract: backend token generation and frontend authorization checks must stay in sync with this table.
- API-local auth details and claim examples are documented in `docs/api/authentication.md`.

## Operations additions

## Current development data mode

- **Database-backed persistence is the default mode in all environments, including deployed app environments.**
- Admin/public/donor controllers now resolve persistence through repository/service interfaces so domain logic is consistent whether data comes from EF Core repositories or approved development fallbacks.
- **In-memory data is development-only and opt-in** via `DevelopmentFeatures:UseInMemoryDataStore=true` (only honored when `ASPNETCORE_ENVIRONMENT=Development`).
- Keep the in-memory flag disabled for staging/production so operational behavior always reflects durable PostgreSQL-backed data.

This repository now includes:

- **CI pipeline** at `.github/workflows/ci.yml` (frontend lint/typecheck/tests/build + backend build/tests).
- **CD staging pipeline** at `.github/workflows/cd-staging.yml` (Azure backend + static frontend deployment).
- **Health checks and telemetry hooks** in `backend/SafeHarbor/SafeHarbor/Program.cs`.
- **Operational runbooks** in `docs/operations/`.
- **HTTPS redirect verification runbook and demo script** in `docs/operations/https-redirect-runbook.md` and `docs/operations/demo-https-redirect-script.md`.
- **Non-technical admin SOPs** in `docs/admin/non-technical-admin-guide.md`.
- **Starter telemetry dashboard JSON** in `infra/azure/staging-telemetry-dashboard.json`.


## Secrets configuration (local + cloud)

Tracked config files (`appsettings.json` and `appsettings.Development.json`) now contain placeholders only for sensitive values. Set real secrets in one of the supported secret providers below.

| Source | When to use | Keys |
|---|---|---|
| Environment variables | CI/CD, containers, quick local overrides | `ConnectionStrings__DefaultConnection`, `LocalAuth__SigningKey`, `KeyVault__VaultUri`, `DevelopmentFeatures__UseInMemoryDataStore` |
| .NET user-secrets | Local development on trusted machines | `ConnectionStrings:DefaultConnection`, `LocalAuth:SigningKey`, `DevelopmentFeatures:UseInMemoryDataStore` |
| Azure Key Vault | Shared non-local environments | `ConnectionStrings--DefaultConnection`, `LocalAuth--SigningKey` |

### Local bootstrap

1. Copy `backend/SafeHarbor/.env.example` to `.env` (or export equivalent shell variables).
2. Prefer `dotnet user-secrets` for local-only secrets:
   - `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<value>" --project backend/SafeHarbor/SafeHarbor/SafeHarbor.csproj`
   - `dotnet user-secrets set "LocalAuth:SigningKey" "<32+ char key>" --project backend/SafeHarbor/SafeHarbor/SafeHarbor.csproj`
3. For cloud, set `KeyVault:VaultUri` and store secret values in Azure Key Vault.

### Credential/signing-key rotation checklist

If credentials or signing keys were ever committed, rotate immediately:

1. Rotate the PostgreSQL user password/connection string at the database server.
2. Rotate `LocalAuth:SigningKey` (new random 32+ char value) in user-secrets, CI secret store, and Key Vault.
3. Redeploy/restart workloads so old secrets are no longer active in memory.
4. Revoke and replace any dependent app registrations or tokens if they used the exposed secret.
5. Validate startup/auth flows after rotation and remove stale values from local machines.

## Content Security Policy (CSP) allowlist

The API now emits a `Content-Security-Policy` header with strict defaults to reduce cross-site scripting risk while preserving required integrations:

- `default-src 'self'` keeps all resource classes same-origin by default.
- `script-src 'self'` allows only first-party scripts.
- `style-src 'self' https://fonts.googleapis.com` allows bundled CSS plus Google Fonts stylesheet delivery.
- `img-src 'self' data:` allows first-party images and inline data URI assets (for small embedded icons/previews).
- `font-src 'self' https://fonts.gstatic.com data:` allows first-party and Google-hosted font files.
- `connect-src 'self' https://login.microsoftonline.com` allows first-party API calls and Microsoft Entra ID auth-related calls.

Additional hardening directives are also applied (`object-src 'none'`, `base-uri 'self'`, and `frame-ancestors 'none'`) to block legacy plugin content and reduce framing/navigation abuse.

## Data sensitivity and handling model

All entities/fields should be tagged conceptually into one of the following classes during modeling and review:

- **PII:** personal names, addresses, contact details, identifiers.
- **Child-sensitive:** case notes, placements, assessments, welfare/incident details.
- **Financial:** donations, transactions, grant/funding records, payment references.

Required controls for these classes:

- Encrypt data at rest (database, snapshots/backups) and in transit (TLS).
- Enforce least-privilege role checks in API and admin UI flows.
- Log sensitive access/admin actions for auditability.
- Keep secrets out of code and use environment/managed secret stores.
- Apply retention and secure deletion practices appropriate to data class.

## Coding standards

- Use clear, descriptive naming and keep functions/classes focused on one responsibility.
- Follow existing project patterns before introducing new abstractions.
- Keep business logic out of controllers/UI glue code; place it in Application/Domain layers.
- Add comments for non-obvious decisions, assumptions, and tradeoffs.
- Prefer explicit typing and compile-time safety (TypeScript + C# strong typing).
- Treat warnings in core build/lint/test tooling as issues to fix before merge.

## Branching strategy

- `main` is protected and always deployable.
- Create short-lived feature branches from `main` using naming like:
  - `feature/<work-item>`
  - `fix/<work-item>`
  - `chore/<work-item>`
- Rebase or merge from `main` frequently to reduce drift.
- Use pull requests for all changes; no direct pushes to protected branches.

## Database migration policy

- Schema changes must be delivered via versioned migrations.
- Every migration requires:
  - clear intent/name,
  - forward migration steps,
  - rollback/mitigation notes where feasible.
- Migration files are reviewed as first-class code artifacts.
- Production migrations must be applied through CI/CD or approved runbooks, never ad hoc/manual SQL without traceability.

## Required CI checks

A pull request must pass the following checks before merge:

1. **Frontend build + lint + tests**
   - Install dependencies, run lint, run tests, and run production build.
2. **Backend restore/build/tests**
   - Restore dependencies, compile, run automated tests.
3. **Migration validation**
   - Verify new migrations are present/consistent for schema changes and can apply cleanly in CI database context.
4. **Security/static checks**
   - Secret scanning and baseline static analysis.
5. **PR quality gates**
   - At least one reviewer approval and all required status checks green.

## Change control

If a change intentionally deviates from this baseline, document the decision in a new ADR and reference it in the related PR.
