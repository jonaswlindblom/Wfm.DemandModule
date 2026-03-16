# WFM Demand Module - Agent Instructions

## Product goal
Build a WFM Demand Module that transforms customer data streams into workload per work activity and time interval.

## Tech stack
- Frontend: React + TypeScript + Tailwind
- Backend: ASP.NET Core Web API
- Database: SQL Server 2019
- Hosting: IIS
- API versioning: /api/v1
- Auth/RBAC: Admin, Planner, Viewer

## Domain rules
Primary concept:
Stream Event -> Mapping Rules / Engine -> Workload by activity and interval

First use case:
CampingBookingCreated =>
- Reception = 0.3h + 0.05h per addon
- Housekeeping = 0.6h per stay-night, modified by cabinType

## Important constraints
- Single-tenant only
- No tenantId in database tables
- SQL Server 2019 compatibility only
- No TODOs in core logic
- Keep changes small and reviewable
- Prefer vertical slices over broad refactors

## Backend conventions
- Use ASP.NET Core Web API
- Keep business logic out of controllers
- Prefer service classes for engine logic
- Add tests for engine behavior and API behavior
- Explainability must be included where possible

## Frontend conventions
- Keep existing layout unless explicitly asked to change it
- Use React Query for API calls
- Show loading and error states
- Replace mock data incrementally

## Done means
- Code builds
- Tests pass
- Existing UI still renders
- New feature is wired end-to-end
- Brief summary of changed files is included