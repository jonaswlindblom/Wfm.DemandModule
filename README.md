# WFM Demand Module (Backend + Frontend)

This repo contains a full-stack MVP of a **Demand Module** for a WFM system:
- **Backend**: ASP.NET Core Web API (.NET 8 LTS, .NET 10 upgrade-ready), EF Core, SQL Server 2019, Swagger, JWT RBAC, Audit log, Simulation engine (Rules + calibration).
- **Frontend**: React + TypeScript (Vite + MUI + React Router + TanStack Query + Recharts) with **Mapping Editor** and **Simulation** screens.
- **DB**: T-SQL schema + seed data (incl. CampingBookingCreated example).

> Single-tenant by design (no tenantId columns).

## Repo structure

- `src/` backend projects  
- `tests/` xUnit tests  
- `sql/` SQL Server 2019 scripts  
- `iis/` `web.config` for IIS in-process  
- `frontend/` React SPA  

## Quick start (local)

### 1) Database
Run in SQL Server 2019:

1. `sql/001_create_schema.sql`
2. `sql/002_seed_demo_data.sql`

### 2) Backend
Open `Wfm.DemandModule.sln` in Visual Studio 2022+ and run **Wfm.DemandModule.Api**.

Update connection string if needed:
- `src/Wfm.DemandModule.Api/appsettings.json`

Swagger:
- `https://localhost:<port>/swagger`

Create a dev token (for local):
- `POST /api/v1/auth/token` with body `{ "userId":"jonas", "role":"Admin" }`  
Copy the token into Swagger **Authorize** (`Bearer <token>`).

### 3) Frontend
```bash
cd frontend
npm install
npm run dev
```

Set token in browser devtools:
```js
localStorage.setItem("dm_token","<paste token>")
```

Optional: set API base via env:
- `frontend/.env.local` → `VITE_API_BASE=https://localhost:<port>/api/v1`

## GitHub import
1. Create an empty GitHub repo (no README/gitignore).
2. Push this repository contents.

Example:
```bash
git init
git add .
git commit -m "Initial demand module MVP"
git branch -M main
git remote add origin <your-github-repo-url>
git push -u origin main
```

## Notes
- Auth is **DEV token** for fast start; swap to your IdP later.
- Feedback calibration endpoint exists, but MVP uses a conservative default baseHours=1 for updating factor (next step is to pass raw/base from simulation explanations).
