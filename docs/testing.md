# Testing

Purpose: explain what the suites cover and how to run them. Numbers drift as tests are added; treat the categories as the contract.

## How to run

### Backend

Integration tests need a reachable PostgreSQL server (same provider as production):

```bash
docker compose up -d db
cd backend
dotnet test
```

Override the server with `TIMEREADY_TEST_DB` if needed, for example:

```bash
TIMEREADY_TEST_DB="Host=localhost;Port=5433;Username=timeready;Password=timeready" dotnet test
```

Each integration test class gets its own throwaway database name. Unit tests do not need PostgreSQL.

### Frontend

```bash
cd frontend
npm ci
npm test
```

Tests run with Vitest through Angular's unit-test builder.

## What is covered

### Backend unit tests

| Area | Focus |
| --- | --- |
| Rule engine | Every rule, severity, effect on readiness, configured thresholds; fixed `TimeProvider` |
| Token service | Claims, issuer/audience, lifetimes, hashed refresh tokens, rejection of foreign signing keys |
| Audit interceptor | Changed columns only, no-op saves write nothing, audit rows are not audited (in-memory SQLite) |
| Retention | Archiving by age, batching, archive keeps original ids/values, purge stays off unless enabled, status monitor clears after recovery |

### Backend integration tests

`WebApplicationFactory` boots the real API against PostgreSQL (migrations + seeding):

- Sign-in, refresh, refresh-token reuse detection
- Employee CRUD, validation errors, 404 handling
- Readiness evaluation
- Operator may update but not delete
- Audit endpoints: operator gets 403, bad page size gets 400, create/update appears in history
- Retention status / run endpoints

### Frontend tests

Unit coverage for auth service, interceptor, guards and the HR store, plus `auth-flow.spec.ts`, which walks the lifecycle with the real service, interceptor and guards wired together:

- Sign in and bearer attachment
- Expired access token → single shared refresh → request replay
- Admin reaches audit; operator is sent to the no-access page
- Sign out and session restore behaviour after reload

## CI

GitHub Actions (`.github/workflows/ci.yml`) runs:

1. Backend restore, build and `dotnet test` against a Postgres service container
2. Frontend `npm ci`, `npm test` and production build

## Design note

Integration tests use PostgreSQL on purpose. A different engine would hide provider-specific behaviour. See [Decisions](decisions.md).
