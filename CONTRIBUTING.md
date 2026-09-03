# Contributing

Purpose: how to run, test and propose changes to TimeReady. This is a personal portfolio repository; unsolicited large features are rarely useful, but fixes and clear documentation improvements are welcome.

## Prerequisites

- .NET SDK 9.0
- Node.js 20+ (CI uses Node 22)
- Docker (recommended) or a local PostgreSQL 17 instance

## One-command stack

```bash
docker compose up --build
```

| What | URL |
| --- | --- |
| Application | http://localhost:4200 |
| Swagger | http://localhost:5080/swagger |
| Health | http://localhost:5080/health |

Sign in with `admin@timeready.local` / `Admin#Demo2026` or `operator@timeready.local` / `Operator#Demo2026`.

Stop with `docker compose down`. Add `-v` to drop the database volume.

## Running parts separately

```bash
docker compose up -d db

cd backend/TimeReady.Api
dotnet run                 # http://localhost:5080

cd ../../frontend
npm ci
npm start                  # http://localhost:4200
```

Provide `Jwt__SigningKey` (32+ characters) via environment or user secrets outside Development. See [`.env.example`](.env.example) for the production Compose shape.

## Tests

```bash
docker compose up -d db
cd backend && dotnet test

cd ../frontend && npm test
```

Details: [docs/testing.md](docs/testing.md).

## Pull requests

1. Keep changes focused — one concern per PR.
2. Update docs when behaviour or setup changes.
3. Do not commit secrets, `.env` files or real signing keys.
4. Ensure `dotnet test` and `npm test` pass locally when you touch those areas.

## Code style

EditorConfig is at the repository root. Prefer the existing patterns: thin controllers, options validated at startup, policies for authorisation, and no business rules inside HTTP or EF layers.
