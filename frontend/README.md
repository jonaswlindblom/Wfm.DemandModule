# Demand Module Frontend (Prototype)

## Prereqs
- Node.js 18+ (20+ recommended)

## Run
1) Start backend (Swagger at https://localhost:xxxxx/swagger)
2) In this folder:
   npm install
   npm run dev

Default URL: http://localhost:5173

## Configure API base (optional)
Create `.env.local`:
VITE_API_BASE=https://localhost:57194/api/v1

If not set, it uses https://localhost:5001/api/v1 (you can override in UI too via env).

## Auth
Use Swagger to POST /api/v1/auth/token and paste the access token in the top bar in the UI.
