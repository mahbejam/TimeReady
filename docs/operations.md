# Operations

Purpose: how the running system behaves in production-shaped deployments — logging, health, configuration and release caveats. Not a full runbook for a 24/7 platform; enough for an honest demo and a serious review.

## Production concerns

| Concern | How it is handled |
| --- | --- |
| Audit trail | EF interceptor records create/update/delete with old and new values; append-only; Admin-only to read |
| Audit retention | Background job archives aged entries in batches; purge is opt-in; status via API and health check |
| Authentication | Identity with password policy and lockout; JWT + rotating refresh tokens |
| Logging | Serilog request logging; console + daily rolling file under `logs/` (7 files retained) |
| Errors | `IExceptionHandler` + ProblemDetails; every response can carry a `traceId`; stacks stay in logs |
| Validation | FluentValidation → `ValidationProblemDetails` |
| Rate limiting | Fixed window per client address (default 120 / 60 s) → `429` + `Retry-After` |
| Security headers | See [security.md](security.md) |
| Health | `/health`, `/health/live`, `/health/ready` with JSON naming failing checks; exception details stay out of the response body |
| Configuration | Options bound and validated with `ValidateOnStart` — bad config fails at startup |
| API versioning | Asp.Versioning v1.0 default; URL paths stay `/api/...` |

## Compose behaviour

- Base `docker-compose.yml`: production shape — no published DB/API ports, no Swagger, secrets from the environment.
- `docker-compose.override.yml`: local development — ports 5432 and 5080, Development environment, throwaway secrets.
- Startup order uses health checks: API waits for Postgres; web waits for API.
- The web container probes `GET /healthz` on `127.0.0.1` (not `localhost`) so IPv6 resolution cannot mark a working container unhealthy.

## Release caveats (documented on purpose)

1. **Migrations on startup** are convenient for a single-instance demo. A multi-replica production deployment should run `dotnet ef database update` (or an init job) as its own step.
2. **Demo passwords** in the override file must not ship to a shared environment.
3. **Font inlining** is disabled in the Angular production build so `npm run build` works in offline or restricted CI. Fonts load from the `<link>` in `index.html`.

## Related

- [security.md](security.md) — auth and hardening
- [testing.md](testing.md) — what CI proves
- [../.env.example](../.env.example) — required environment for production-shaped Compose
