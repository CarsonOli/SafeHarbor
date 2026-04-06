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

## Operations additions

This repository now includes:

- **CI pipeline** at `.github/workflows/ci.yml` (frontend lint/typecheck/tests/build + backend build/tests).
- **CD staging pipeline** at `.github/workflows/cd-staging.yml` (Azure backend + static frontend deployment).
- **Health checks and telemetry hooks** in `backend/SafeHarbor/SafeHarbor/Program.cs`.
- **Operational runbooks** in `docs/operations/`.
- **Non-technical admin SOPs** in `docs/admin/non-technical-admin-guide.md`.
- **Starter telemetry dashboard JSON** in `infra/azure/staging-telemetry-dashboard.json`.

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
