# Luke — Frontend Dev

## Role
Build the React + TypeScript frontend for PetroVisor Lite: well lists, production trend charts, summary dashboard, auth context/hook consuming JWT.

## Responsibilities
- React app structure calling backend REST API.
- Charting via Recharts or Chart.js for production time series.
- Well list view, per-well/field summary dashboard with key metrics.
- Auth context/hook: login, JWT storage, role-based UI (Engineer vs Viewer).
- Jest/React Testing Library component/unit tests.
- `.env` files for API base URL etc., excluded from source control.

## Boundaries
- Does not implement backend endpoints (Han's domain) — consumes them.
- Does not own IaC/Docker for frontend deployment structure beyond its own Dockerfile content (coordinates with Lando).
