# API Authentication and Role Mapping

This document defines the local-auth JWT contract used by the SafeHarbor API and frontend.

## Role mapping contract (source of truth)

Local-auth users are persisted in PostgreSQL (`lighthouse.users.role`) with lowercase DB roles. JWT generation maps those DB roles to app authorization roles:

| Database role | App role emitted in JWT (`role`, `roles`, `ClaimTypes.Role`) |
|---|---|
| `admin` | `Admin` |
| `staff` | `SocialWorker` |
| `user` | `Donor` |

### Why this matters

- API authorization policies evaluate app roles (`Admin`, `SocialWorker`, `Donor`).
- Frontend `ProtectedRoute` and `authSession` must check against app roles.
- The JWT also includes `db_role` so diagnostics can confirm what persisted role produced the mapped app role.

## Endpoints

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me`

## Request role input compatibility

To reduce client drift while preserving the DB contract, `/api/auth/register` and optional `/api/auth/login` role input accept either vocabulary:

- DB values: `admin`, `staff`, `user`
- App values: `Admin`, `SocialWorker`, `Donor`

The API normalizes to DB values before persistence/comparison.

## JWT claims emitted by `/api/auth/login`

The local-auth token includes:

- identity claims: `email`, `preferred_username`, `sub`
- DB traceability claim: `db_role`
- app authorization claims (same mapped app role in all three claim types):
  - `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`
  - `role`
  - `roles`

## Frontend expectations

- `authSession.roles` contract is app-role based: `Admin`, `SocialWorker`, `Donor`.
- Frontend JWT parsing should normalize DB-role values to app roles before route checks, so legacy tokens/storage payloads do not bypass or break authorization behavior.
