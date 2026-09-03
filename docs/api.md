# API reference

Purpose: endpoint map for explorers and for the HTTP file in the backend. Prefer Swagger at runtime when the Development stack is up.

Base URL in local Compose/development: `http://localhost:5080`

Interactive docs: `http://localhost:5080/swagger` (Development only)

Ready-made requests: [`backend/TimeReady.Api/TimeReady.Api.http`](../backend/TimeReady.Api/TimeReady.Api.http)

## Demo accounts (Development)

| Account | Role | Password |
| --- | --- | --- |
| `admin@timeready.local` | Admin | `Admin#Demo2026` |
| `operator@timeready.local` | Operator | `Operator#Demo2026` |

In Swagger, use **Authorize** and paste the `accessToken` from `POST /api/auth/login`.

## Endpoints

| Method | Route | Required role | Purpose |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | – | Sign in; returns access and refresh tokens |
| POST | `/api/auth/refresh` | – | Exchange a refresh token for a new pair |
| POST | `/api/auth/logout` | any | Revoke a refresh token |
| GET | `/api/auth/me` | any | Signed-in account and roles |
| GET | `/api/employees` | Admin, Operator | List employees (upcoming vacations first) |
| GET | `/api/employees/{id}` | Admin, Operator | Single employee |
| POST | `/api/employees` | Admin | Create employee |
| PUT | `/api/employees/{id}` | Admin, Operator | Update employee |
| DELETE | `/api/employees/{id}` | Admin | Delete employee |
| GET | `/api/readiness` | Admin, Operator | Readiness for every employee |
| GET | `/api/readiness/{employeeId}` | Admin, Operator | Readiness for one employee |
| POST | `/api/readiness/evaluate` | Admin, Operator | Preview readiness for unsaved form data |
| GET | `/api/audit` | Admin | Search audit trail (filtered, paged) |
| GET | `/api/audit/{id}` | Admin | Single audit entry |
| GET | `/api/audit/employees/{employeeId}` | Admin | Change history for one employee |
| GET | `/api/audit/archive` | Admin | Search archived entries |
| GET | `/api/audit/retention` | Admin | Retention policy, job status, table sizes |
| POST | `/api/audit/retention/run` | Admin | Apply retention immediately |
| GET | `/health` | – | Overall status |
| GET | `/health/live` | – | Liveness probe |
| GET | `/health/ready` | – | Readiness probe (includes database) |

## Error shape

| Situation | Status | Body |
| --- | --- | --- |
| Validation failure | 400 | `ValidationProblemDetails` |
| Missing record | 404 | ProblemDetails |
| Unauthenticated / bad token | 401 | – |
| Authenticated but not allowed | 403 | – |
| Rate limited | 429 | `Retry-After` header |
| Unhandled failure | 500 | ProblemDetails with `traceId` (details in logs only) |

API versioning defaults to v1.0 via `Asp.Versioning` (`X-Api-Version`, `api-version` query, or media type). URL paths stay `/api/...` so existing clients keep working.

## Readiness rule codes

| Code | Severity | Triggered when |
| --- | --- | --- |
| `negative-time-balance` | Critical / Warning | Balance at or below -20 h / -8 h (configurable) |
| `vacation-starts-soon` | Warning | Vacation starts within 7 days (configurable) |
| `low-vacation-days` | Info | Fewer than 3 vacation days remain (configurable) |
| `manager-not-informed` | Critical | Manager flag is not set |
| `handover-incomplete` | Critical | Handover flag is not set |
| `no-vacation-planned` | Info | No vacation start date |

## Audit search example

```http
GET /api/audit?entityName=Employee&action=Updated&user=admin@timeready.local&from=2026-07-01T00:00:00Z&page=1&pageSize=25
```

Page size is capped at 100.
