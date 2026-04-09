# HTTPS Redirect and Forwarded Headers Runbook

This runbook defines how to keep SafeHarbor HTTPS enforcement working correctly when the API is hosted behind a cloud reverse proxy/load balancer (for example, Azure App Service ingress).

## Why this runbook exists

- SafeHarbor now enforces `app.UseHttpsRedirection()` in the API pipeline.
- In cloud environments, TLS often terminates at the proxy, so the app receives proxied HTTP unless `X-Forwarded-Proto` is processed first.
- If forwarded headers are not honored, the app can mis-detect scheme and either skip redirects incorrectly or loop redirects.

## Required middleware behavior

The backend middleware order in `Program.cs` must remain:

1. `app.UseForwardedHeaders()`
2. `app.UseHttpsRedirection()`

> NOTE: This order is required so the app reads the original client scheme before redirect logic executes.

## Deployment checklist (staging/prod)

1. Deploy backend revision containing the forwarded-headers + HTTPS-redirection middleware sequence.
2. Confirm ingress/proxy preserves `X-Forwarded-Proto` and `X-Forwarded-For` (default behavior in Azure App Service front-ends).
3. Validate redirect behavior with the verification commands below.
4. Record result in deployment notes with timestamp and environment.

## Verification commands

Use your deployed host name (example: `safeharbor-api-staging.azurewebsites.net`).

```bash
# Expect redirect (301/302/307/308) and Location: https://...
curl -I http://safeharbor-api-staging.azurewebsites.net/health/live

# Expect direct app response without an extra redirect when proxy says original request was HTTPS.
curl -I \
  -H "X-Forwarded-Proto: https" \
  http://safeharbor-api-staging.azurewebsites.net/health/live
```

### Pass criteria

- First command returns a redirect status with `Location` set to `https://...`.
- Second command does **not** issue an additional HTTP->HTTPS redirect because scheme is already forwarded as HTTPS.

## Rollback guidance

If unexpected redirect loops appear after deployment:

1. Verify proxy/header forwarding path first (preferred fix).
2. If immediate mitigation is required, roll back to the previous known-good backend artifact.
3. Open incident follow-up to correct forwarding configuration before next rollout.
