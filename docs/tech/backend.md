# Backend — Decided (D-62..64)
> Read when: backend work. Principles A1–A11: `architecture.md`.

## Stack
- ASP.NET Core .NET 10 Web API; references `shared/` as a project.
- PostgreSQL via EF Core + Npgsql; migrations in `server/`.
- Docker Compose for local dev (server + Postgres).
- Logging: Serilog. Dev only: built-in OpenAPI + Scalar UI. No client codegen (DTOs live in `shared/`).
- Tests: xUnit + Testcontainers (real Postgres). Rules tested in `shared.Tests`.
- Later: Redis (cache, rate limits, SignalR backplane), SignalR (live PvP), Sentry.

## Auth
- Client signs in with Unity Authentication (anonymous first, account linking later) and sends its access token.
- Server validates the token (Unity JWKS: signature, issuer, audience, expiry) and maps the Unity player ID to `players`.

## Code shape
- Folders per module (A11): `Accounts`, `Champions`, `Inventory`, `Campaign`, `Battles`, `Summons`.
- Endpoint → module service → `shared` rule. One DB transaction per intent, ledger row included (A7).
- Request ID table for safe retries (A6). Player row version for concurrent requests (optimistic concurrency).
- Player state tables: players, champions, gear, inventory (item_id, qty), currencies, campaign_progress, ledger, requests.
- Content: server loads `content/*.json` at startup; content version checked at login (A8).

## Status
Not started (M0-4). Record API in `api.md`, schema in `data.md` as built.
