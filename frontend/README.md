# React + TypeScript + Vite

## Local auth setup

The login page uses a single email/password flow against the backend auth endpoint (`POST /api/auth/login`).
Copy `frontend/.env.example` to `frontend/.env.local` before running locally:

```bash
cp .env.example .env.local
```

Common local settings:

- `VITE_API_BASE_URL=https://localhost:7217` (or your backend local URL)
- `VITE_ENABLE_DEV_ROLE_SIMULATION=true` (optional, for non-login role testing UI if needed)

The backend switch is controlled by `LocalAuth:Enabled` in `backend/SafeHarbor/SafeHarbor/appsettings.Development.json`.

For deployed environments, either set `VITE_API_BASE_URL` to your backend API host or configure your static host to proxy
`/api/*` requests. Without one of those, auth requests can land on the frontend host and return `404/405` instead of reaching
the API.

As a safety net for Azure Static Web Apps deployments, local auth also falls back to known backend App Service hosts
when no `VITE_API_BASE_URL` is provided and same-origin `/api/*` returns `404/405`, including
`https://safeharborbackend-ggdyhzdggag9d3df.canadacentral-01.azurewebsites.net`.
Set `VITE_API_BASE_URL` in CI/CD to the correct backend host for each environment.

Seeded local accounts are available for smoke testing:

- `alice@example.com` / `Password123!` (Donor)
- `admin@safeharbor.local` / `Password123!` (Admin)

## API fallback policy (deployment safety)

Frontend mock fallbacks are **dev-only and opt-in**. This prevents deployment builds from silently shipping fallback/mock payload behavior when backend endpoints fail.

Current fallback flags:

- `VITE_ENABLE_DONOR_DASHBOARD_DEV_FALLBACK`
- `VITE_ENABLE_DONOR_ANALYTICS_DEV_FALLBACK`
- `VITE_ENABLE_IMPACT_DEV_FALLBACK`

Policy requirements:

- Keep all fallback flags unset or `false` in CI/CD and deployed environments.
- Only enable fallback flags in local development (`.env.local`) when intentionally running frontend without a backend.
- If a backend endpoint is unavailable in deployed environments, UI should show an explicit API error (including endpoint + HTTP status) rather than rendering seeded/mock data.

This policy ensures missing integrations are visible during QA and release validation.

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Oxc](https://oxc.rs)
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/)

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend updating the configuration to enable type-aware lint rules:

```js
export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...

      // Remove tseslint.configs.recommended and replace with this
      tseslint.configs.recommendedTypeChecked,
      // Alternatively, use this for stricter rules
      tseslint.configs.strictTypeChecked,
      // Optionally, add this for stylistic rules
      tseslint.configs.stylisticTypeChecked,

      // Other configs...
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```

You can also install [eslint-plugin-react-x](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-x) and [eslint-plugin-react-dom](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-dom) for React-specific lint rules:

```js
// eslint.config.js
import reactX from 'eslint-plugin-react-x'
import reactDom from 'eslint-plugin-react-dom'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...
      // Enable lint rules for React
      reactX.configs['recommended-typescript'],
      // Enable lint rules for React DOM
      reactDom.configs.recommended,
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```
