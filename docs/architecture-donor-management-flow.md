# Donor Management Architecture Note

## Canonical flow

SafeHarbor now uses **`/app/donors` → `DonorsContributionsPage`** as the single canonical donor-management entry point.

This page is backed by `adminOperationsApi` contracts (`/api/admin/donors-contributions/donors`) and typed frontend models from `frontend/src/types/adminOperations.ts`.

## Why this is canonical

- It follows the shared admin-operations pattern used by other `/app/*` staff pages (caseload, process recording, and visitation).
- It uses explicit typed DTO contracts rather than ad-hoc `Record<string, unknown>` shapes.
- It removes duplicate route/page drift where multiple staff routes previously represented the same donor management concern.

## Route policy

- Keep `main.tsx` mapping for donor management on **`/app/donors`**.
- Do not reintroduce alternate donor-management route aliases unless there is a documented product requirement and migration plan.

## Maintenance guardrail

When extending donor-management features, prefer:

1. Extending `adminOperationsApi` and `adminOperations` types first.
2. Keeping presentation logic in `DonorsContributionsPage`.
3. Avoiding parallel legacy service/page pairs for the same domain workflow.
