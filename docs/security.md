# Security

Purpose: summarise how TimeReady protects data and where the current design still has deliberate limits. For vulnerability reports, see the root [SECURITY.md](../SECURITY.md).

## Authentication

- ASP.NET Core Identity stores accounts and password hashes.
- Sign-in returns a short-lived JWT access token and a longer-lived refresh token.
- Password policy requires at least 10 characters; lockout engages after 5 failed attempts.
- The JWT signing key is never committed. Startup fails if it is missing or shorter than 32 characters.

## Refresh tokens

- Only the SHA-256 hash of a refresh token is stored.
- Tokens rotate on every refresh; the previous token is revoked.
- Presenting an already-rotated token is treated as reuse (possible theft): remaining sessions for that user are revoked.

## Authorisation

| Role | Access |
| --- | --- |
| Operator | Read employees and readiness; update existing employees |
| Admin | Operator permissions plus create/delete employees and full audit access |

Controllers enforce **policies**, not scattered role string checks. The Angular UI hides actions the API would reject; the API still enforces every call.

## Audit and retention

- Employee create/update/delete is append-only audited (who, when, which fields, old/new values, trace id).
- Passwords and tokens are not written to the audit trail.
- Retention archives old entries; permanent purge is off by default.
- Audit read endpoints are Admin-only.

## HTTP hardening

| Control | Behaviour |
| --- | --- |
| Security headers | `nosniff`, frame deny, no-referrer, restrictive CSP outside Swagger; HSTS outside Development |
| Rate limiting | Fixed window per client address (default 120 / 60 s) → `429` + `Retry-After` |
| Errors | ProblemDetails with `traceId`; exception details stay in logs |
| CORS | Explicit allowed origins from configuration |
| Health | `/health`, `/health/live`, `/health/ready` (DB check) for orchestration; responses omit exception messages |

## Secrets and environments

- Development Compose override injects throwaway passwords and a development signing key so `docker compose up` works without a `.env` file.
- Production-shaped Compose (`docker-compose.yml` alone) expects values from the environment — see [`.env.example`](../.env.example).
- Demo accounts are for local evaluation only. Change passwords before any shared or internet-facing deployment.

## Known limitations (honest scope)

1. Refresh token in `sessionStorage` is weaker than an HttpOnly cookie (see [Decisions](decisions.md)).
2. There is no organisation tenancy model; one deployment is one logical workspace.
3. Rate limiting is per client address — fine for a demo and small deployments, not a full WAF.
4. Migrations-on-startup is convenient for demos; production should separate migrate from serve.

None of these are hidden defects presented as features — they are documented follow-ups.
