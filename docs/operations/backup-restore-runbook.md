# Backup and Restore Runbook

## Purpose

This runbook defines the minimum safe process for backing up and restoring SafeHarbor staging/production data.

## Scope

- Azure Database for PostgreSQL (primary relational datastore)
- Application configuration in Azure App Service / Azure Static Web Apps
- GitHub Actions environment secrets used for deployments

---

## Backup schedule

### Database backups

1. **Automated backups**
   - Enable Azure Database for PostgreSQL automatic backups.
   - Retention target:
     - Staging: 7 days
     - Production: 35 days
2. **Weekly logical export**
   - Run `pg_dump` weekly and store encrypted `.dump` in approved storage account container.
3. **Pre-release backup**
   - Trigger an on-demand backup before any schema migration to production.

### Configuration backups

1. Export App Service app settings and connection strings as JSON weekly.
2. Export Static Web App environment settings weekly.
3. Export GitHub repository/environment secret inventory metadata (names only, no values) monthly.

---

## Restore process (database)

### Preconditions

- Incident ticket opened and severity assigned.
- Product owner confirms recovery point objective (RPO) and acceptable data window.

### Steps

1. Identify recovery timestamp and environment (staging or production).
2. Restore PostgreSQL server to a **new** instance using point-in-time restore.
3. Run smoke queries to validate schema and row counts for key tables.
4. Update backend connection string in target environment to restored instance.
5. Run API health probes:
   - `GET /health/live`
   - `GET /health/ready`
6. Validate critical admin workflows (user management + donor CRUD + case CRUD).
7. Capture incident notes including data-loss window and remediation actions.

---

## Restore process (configuration)

1. Pull latest approved configuration snapshot from secure storage.
2. Re-apply:
   - App Service app settings
   - Connection strings
   - Static Web App app settings
3. Re-run deployment from `main` branch if binaries are suspected stale.
4. Confirm frontend and backend health checks.

---

## Post-restore validation checklist

- [ ] Admin login succeeds.
- [ ] Role-based access still enforced.
- [ ] Core CRUD workflows complete without 5xx errors.
- [ ] Telemetry dashboard receives fresh requests, failures, and latency signals.
- [ ] Audit logs include restore window activity.

---

## Ownership

- **Primary:** DevOps lead
- **Secondary:** Engineering manager
- **Business sign-off:** Operations director

