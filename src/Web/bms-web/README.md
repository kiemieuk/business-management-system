# BMS Web

React + TypeScript (Vite) frontend for the Business Management System.

## Development

```bash
npm install
npm run dev      # http://localhost:5173, /api proxied to http://localhost:5000
```

Override the proxy target with `VITE_PROXY_TARGET` (e.g. in `.env.local`).

## Scripts

- `npm run build` – type-check and build to `dist/`
- `npm run lint` – lint with oxlint

## Structure

- `src/api` – fetch client (`client.ts`) and typed API modules per backend service
- `src/types` – DTO types
- `src/auth` – auth context, `ProtectedRoute`
- `src/layout` – app shell; navigation is configured in `src/layout/navigation.ts`
- `src/features/<module>` – feature pages (add new business modules as new folders and register their routes in `App.tsx` and nav items in `navigation.ts`)

## Docker

`docker build -t bms-web .` – serves the SPA via nginx and proxies `/api/` to `http://api-gateway:8080`.
