# Decisions

Purpose: record the design choices that matter for reviewers. These are not a full ADR catalogue — only decisions that shape how the product behaves or how it would change later.

## 1. Rule-based readiness, not ML

**Decision:** Evaluate vacation readiness with explicit thresholds and flags.

**Why:** HR needs to explain a "not ready" result. A small rule set is auditable, configurable without a redeploy of business logic, and cheap to test. There is no training data and no third-party AI cost.

**Consequence:** Findings are structured (`code`, `severity`, `message`, `recommendation`). A future LLM layer could summarise those findings in prose; it would not become the authority for readiness.

## 2. Readiness service has no HTTP or database dependency

**Decision:** `IReadinessService` takes an `Employee` (and configuration / `TimeProvider`) and returns a `ReadinessResult`.

**Why:** Rules can be unit-tested without a web host. Controllers and repositories can change without rewriting policy. A different persistence model would not force a rewrite of the decision logic.

## 3. Injected `TimeProvider` instead of `DateTime.Now`

**Decision:** Date-sensitive rules use `TimeProvider`.

**Why:** "Vacation starts in 3 days" must be reproducible in CI. Fixed clocks keep edge cases (today, imminent window, far-future dates) deterministic.

## 4. Policies over hard-coded role checks in controllers

**Decision:** Endpoints require policies such as `employees:read` / `employees:manage`. Roles map to policies in one place.

**Why:** A third role can be introduced without touching every controller. The UI mirrors the same idea with route guards, but the API remains the source of truth.

## 5. Audit via `SaveChangesInterceptor`

**Decision:** Record create/update/delete in an EF interceptor, not in controllers.

**Why:** Background jobs, new endpoints and admin tools all go through `SaveChanges`. Relying on developers to remember a log call is fragile for an audit trail.

**Trade-off:** Identity tables (accounts, roles, refresh tokens) are not audited as employee changes. Passwords and tokens are never written to the audit store.

## 6. Archive by default, purge opt-in

**Decision:** Retention moves old entries to an archive table. Permanent deletion is disabled unless explicitly enabled, and configuration refuses combinations that would delete before archiving.

**Why:** An append-only trail that only grows will eventually hurt operational queries. Archiving keeps the live table small while history stays searchable. Irreversible deletion should be a conscious ops choice.

## 7. Access token in memory, refresh token in `sessionStorage`

**Decision:** The SPA keeps the access token in memory and the refresh token in `sessionStorage`.

**Why:** Closing the tab clears the refresh token. The access token is never written to durable browser storage. Rotation + reuse detection on the server limits the damage of a stolen refresh token.

**Known limitation:** Any token JavaScript can read, injected script can read. The stronger follow-up is an `HttpOnly; Secure; SameSite=Strict` cookie for the refresh token.

## 8. Migrations on startup for the demo stack

**Decision:** The API applies pending EF migrations when it starts.

**Why:** One-command local and Compose demos stay reliable.

**Trade-off:** A real multi-instance production deployment should run migrations as a separate release step (`dotnet ef database update` or an init job), not from every replica at boot.

## 9. PostgreSQL for integration tests

**Decision:** Integration tests boot the real app with `WebApplicationFactory` against a throwaway PostgreSQL database per test class.

**Why:** The production provider is PostgreSQL. Testing against a different engine hides provider-specific behaviour. CI starts Postgres as a service; locally `docker compose up -d db` is enough.

Unit tests that only need a change tracker (audit interceptor) still use in-memory SQLite where that is sufficient.
