# WFM Demand Module (Backend + Frontend)

This repo contains a full-stack MVP of a Demand Module for a WFM system:
- Backend: ASP.NET Core Web API (.NET 8), EF Core, SQL Server 2019, Swagger, JWT RBAC, audit log, simulation engine.
- Frontend: React + TypeScript in `frontend.UI` (Vite + MUI + React Query + Recharts).
- DB: T-SQL schema + seed data, including the `CampingBookingCreated` example.

Single-tenant by design: no `tenantId` columns.

## Repo Structure

- `src/` backend projects
- `tests/` xUnit tests
- `sql/` SQL Server 2019 scripts
- `iis/` IIS config
- `frontend.UI/` React SPA

## Quick Start

### 1) Database

Run in SQL Server 2019:

1. `sql/001_create_schema.sql`
2. `sql/002_seed_demo_data.sql`

### 2) Backend

Open `Wfm.DemandModule.sln` in Visual Studio 2022+.

Recommended startup profiles:
- `API Only` for backend debugging in Visual Studio.
- `API + Frontend UI` only when the JavaScript project tooling is installed and working.

If Visual Studio shows `Unable to launch the selected debugger. Please choose another.`, switch the solution startup profile to `API Only` and start the frontend separately with `npm run dev`.

Update connection string if needed:
- `src/Wfm.DemandModule.Api/appsettings.json`

Swagger:
- `https://localhost:<port>/swagger`

Create a dev token:
- `POST /api/v1/auth/token` with body `{ "userId": "jonas", "role": "Admin" }`

### 3) Frontend

```bash
cd frontend.UI
npm install
npm run dev
```

Set token in browser devtools:

```js
localStorage.setItem("dm_token", "<paste token>")
```

Optional: set API base via env:
- `frontend.UI/.env.local` -> `VITE_API_BASE=https://localhost:<port>/api/v1`

## Notes

- Auth is a dev token flow for fast local startup.
- The frontend is also included in the solution as `frontend.ui.esproj`.
