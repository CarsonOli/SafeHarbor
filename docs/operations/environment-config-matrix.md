# Environment Configuration Matrix

| Setting | Local Dev | CI | Staging | Production | Notes |
|---|---|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | `CI` | `Staging` | `Production` | Drives ASP.NET behavior. |
| `ConnectionStrings__SafeHarborDb` | Local SQLite/PostgreSQL | Ephemeral DB or SQLite | Azure PostgreSQL staging | Azure PostgreSQL prod | Never commit credentials. |
| `AzureAd__Instance` | Optional/local | Mock or disabled | Required | Required | Entra tenant authority URL. |
| `AzureAd__TenantId` | Optional/local | Secret | Secret | Secret | Identity tenant id. |
| `AzureAd__ClientId` | Optional/local | Secret | Secret | Secret | API app registration id. |
| `Telemetry__ServiceName` | `safeharbor-api-dev` | `safeharbor-api-ci` | `safeharbor-api-staging` | `safeharbor-api` | Used in traces/metrics resource labels. |
| `Telemetry__OtlpEndpoint` | Empty or local collector | Empty | Azure Monitor OTLP endpoint | Azure Monitor OTLP endpoint | Enables OpenTelemetry export when populated. |
| `AZURE_BACKEND_APP_NAME_STAGING` | N/A | N/A | Required | N/A | GitHub Actions environment variable. |
| `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` | N/A | N/A | Required | N/A | GitHub Actions environment secret. |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_STAGING` | N/A | N/A | Required | N/A | GitHub Actions environment secret. |

## Change policy

- Any new production setting must be added to this matrix in the same pull request.
- Secrets are stored in Azure/GitHub environments, never in repo files.
- Staging values should mirror production shape (not necessarily scale) to reduce deployment drift.

