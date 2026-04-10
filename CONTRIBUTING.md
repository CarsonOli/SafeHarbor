# SafeHarbor Contributing Guidelines

## Team Rules — Read Before Every PR

### Database
- Never connect to Azure PostgreSQL from frontend/browser code.
- All DB access goes through the .NET backend (EF Core / SafeHarborDbContext) only.
- Never hardcode DB credentials. Use environment variables or Azure Key Vault.
- All tables are in the `lighthouse` schema. Use fully qualified names: `lighthouse.residents`, etc.
- SSL is required on all connections.

### Frontend Deploy Guardrails
- Never modify `frontend/index.html` Vite build behavior.
- Never remove `app_build_command: npm run build` from any SWA workflow file.
- Run these before every push:
```
cd frontend
npm run typecheck
npm run build
```
  If either fails, do not push.
- Never reference raw `.tsx` source files from deployment config — only built `dist/` output.
- If you change any function signature in `AuthContext`, a service file, or a shared type, update every call site in the same PR.

### Auth / API Contract Safety
- If `AuthContext` APIs change, update all call sites in the same PR.
- If service function signatures change, update all callers in the same PR.

### PR Checklist
Before opening a PR, confirm:
- [ ] SWA workflow still includes `app_build_command: npm run build`
- [ ] `npm run typecheck` passes locally
- [ ] `npm run build` passes locally
- [ ] No raw `/src/*.tsx` references in deployment config
- [ ] No DB credentials in source files
- [ ] New SQL queries use `lighthouse.` schema prefix
