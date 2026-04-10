# Logged-in permission matrix (controller-policy source of truth)

This matrix maps current logged-in frontend pages to backend controller policies as of **April 10, 2026**.
It is intended to keep route visibility and route guards aligned with API authorization behavior.

## Policies used by logged-in pages

- `PolicyNames.StaffOrAdmin`: `Admin` and `SocialWorker`
- `PolicyNames.AdminOnly` (or `[Authorize(Roles = "Admin")]`): `Admin` only
- `PolicyNames.SocialWorkerOnly`: `SocialWorker` only
- `PolicyNames.DonorOnly`: `Donor` only

## Matrix

| Logged-in page | Primary endpoint(s) | Backend policy | Admin | SocialWorker | Donor |
|---|---|---|---:|---:|---:|
| `/app/dashboard` | `GET /api/admin/dashboard` | `StaffOrAdmin` | ✅ | ✅ | ❌ |
| `/app/donors` | `GET/POST /api/admin/donors-contributions/donors` | `StaffOrAdmin` | ✅ | ✅ | ❌ |
| `/app/donor-analytics` | `GET /api/admin/donor-analytics` | `AdminOnly` | ✅ | ❌ | ❌ |
| `/app/caseload` | `GET /api/admin/caseload/residents` | `StaffOrAdmin` | ✅ | ✅ | ❌ |
| `/app/contributions` | `GET/PUT/DELETE /api/admin/contributions` | `AdminOnly` (`Roles="Admin"`) | ✅ | ❌ | ❌ |
| `/app/process-recording` (read list) | `GET /api/admin/process-recordings` | `StaffOrAdmin` | ✅ | ✅ | ❌ |
| `/app/process-recording` (create/update/delete) | `POST/PUT/DELETE /api/admin/process-recordings` | `SocialWorkerOnly` | ❌ | ✅ | ❌ |
| `/app/visitation-conferences` | `GET /api/admin/visitation-conferences/*` | `StaffOrAdmin` | ✅ | ✅ | ❌ |
| `/app/reports` | `GET /api/admin/reports-analytics` | `StaffOrAdmin` | ✅ | ✅ | ❌ |
| `/donor/dashboard` | `GET /api/donor/dashboard` | `DonorOnly` | ❌ | ❌ | ✅ |

## Notes

- UI navigation should only render links where the current role has at least one valid API path for the page.
- Even with filtered navigation, explicit route guards must remain in place because users can still deep-link directly.
