# Demo Script: HTTP to HTTPS Redirect Validation

Use this script during staging/prod demos to show SafeHarbor enforces HTTPS when requests enter over plain HTTP.

## Demo setup

- Environment: deployed SafeHarbor API (staging or production).
- Example host: `safeharbor-api-staging.azurewebsites.net`.
- Tooling: `curl` from any shell.

## Live demo steps

1. Explain expected behavior:
   - HTTP requests should be redirected to HTTPS.
   - Requests that already originated as HTTPS at the proxy should not be re-redirected by the app.

2. Run HTTP check:

   ```bash
   curl -I http://safeharbor-api-staging.azurewebsites.net/health/live
   ```

   Narration:
   - Show redirect status (`301/302/307/308`).
   - Point to `Location: https://safeharbor-api-staging.azurewebsites.net/health/live`.

3. Run forwarded-HTTPS check:

   ```bash
   curl -I \
     -H "X-Forwarded-Proto: https" \
     http://safeharbor-api-staging.azurewebsites.net/health/live
   ```

   Narration:
   - Explain this simulates a TLS-terminated reverse proxy forwarding the original HTTPS scheme.
   - Show that the response is not another redirect loop.

4. Close demo with control statement:
   - SafeHarbor honors forwarded headers first, then applies HTTPS redirection logic.

## Demo evidence capture

- Save command output to deployment ticket/notes.
- Record environment, date/time (UTC), and operator.
