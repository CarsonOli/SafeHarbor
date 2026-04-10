# Environment Configuration Matrix

| Setting | Local Dev | CI | Staging | Production | Notes |
|---|---|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | `CI` | `Staging` | `Production` | Drives ASP.NET behavior. |
| `ConnectionStrings__DefaultConnection` | Local PostgreSQL | Ephemeral PostgreSQL | Azure PostgreSQL staging | Azure PostgreSQL prod | Default API connection string key used by EF Core. |
| `DevelopmentFeatures__UseInMemoryDataStore` | Optional (`false` by default) | `false` | `false` | `false` | Dev-only fallback; ignored outside `Development`. |
| `Jwt__Issuer` | `safeharbor-local` | `safeharbor-ci` | Secret | Secret | Token issuer for API JWT validation. |
| `Jwt__Audience` | `safeharbor-local-client` | `safeharbor-ci-client` | Secret | Secret | Token audience for API JWT validation. |
| `Jwt__SigningKey` | Secret | Secret | Secret | Secret | Symmetric signing key (32+ chars). |
| `LocalAuth__Enabled` | `true` | `false` | `false` | `false` | Development-only switch; ignored outside `Development`. |
| `Telemetry__ServiceName` | `safeharbor-api-dev` | `safeharbor-api-ci` | `safeharbor-api-staging` | `safeharbor-api` | Used in traces/metrics resource labels. |
| `Telemetry__OtlpEndpoint` | Empty or local collector | Empty | Azure Monitor OTLP endpoint | Azure Monitor OTLP endpoint | Enables OpenTelemetry export when populated. |
| `AZURE_BACKEND_APP_NAME_STAGING` | N/A | N/A | Required | N/A | GitHub Actions environment variable. |
| `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING` | N/A | N/A | Required | N/A | GitHub Actions environment secret. |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_STAGING` | N/A | N/A | Required | N/A | GitHub Actions environment secret. |
| `AZURE_CLIENT_ID` | N/A | N/A | N/A | Required | GitHub Actions secret for OIDC-based `azure/login` in production deploy workflow. |
| `AZURE_TENANT_ID` | N/A | N/A | N/A | Required | GitHub Actions secret for OIDC-based `azure/login` in production deploy workflow. |
| `AZURE_SUBSCRIPTION_ID` | N/A | N/A | N/A | Required | GitHub Actions secret for OIDC-based `azure/login` in production deploy workflow. |

## Change policy

- Any new production setting must be added to this matrix in the same pull request.
- Secrets are stored in Azure/GitHub environments, never in repo files.
- Staging values should mirror production shape (not necessarily scale) to reduce deployment drift.
