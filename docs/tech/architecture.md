# Architecture — Decided (D-62..65)
> Read when: cross-cutting design, deciding where code goes. Stack details: `backend.md`, `unity.md`.

## Repo layout (monorepo)
```
/CLAUDE.md          Claude guide + doc index
/docs/              GDD, tech, plan, status
/content/           Game content data (JSON): champions, skills, gear, stages, dungeons, balancing
/shared/            Engine-free C# library (netstandard2.1): rules, formulas, combat sim, content models, API DTOs
/shared.Tests/      xUnit tests for shared — `dotnet test`, no Unity
/server/            ASP.NET Core .NET 10 + PostgreSQL (Docker)
/client/            Unity 6 project (2D URP, uGUI, Android)
/scripts/           Local scripts: test, build-android, copy shared DLL + content (D-64)
```

## Key principles
- **A1 Shared rules.** All game rules (stat calc, level/rank costs, merge, awakening, rewards, combat sim) live once in `shared/`, used by both client (preview/UI) and server (authority). Rule IDs from GDD go in comments/tests.
- **A2 Server authority.** Server validates and applies every value-changing action: summons, rewards, upgrades, spends. Client sends *intents* (`RankUpChampion(id)`), server returns new state delta.
- **A3 Deterministic combat.** Combat sim is pure C# with a seeded RNG. Server issues a battle seed; client plays; client submits inputs/result; server re-simulates to verify before granting rewards.
- **A4 Data-driven.** Balancing values come from `content/` (versioned). No magic numbers in code.
- **A5 Testability.** `shared/` is tested with plain `dotnet test`. Unity code stays thin: presentation + input + API calls.
- **A6 Safe retries.** Every value-changing request carries a client-made request ID; server applies each ID once and replays the stored response on repeat.
- **A7 Ledger.** Every grant/spend writes a ledger row (player, delta, reason, request ID).
- **A8 Content version.** Client ships with `content/`; login compares content version with server; mismatch → force update.
- **A9 Client layers.** Core (state, content, net/auth) → Screens (screen logic, plain C#) → Views (uGUI MonoBehaviours). Screens never reference uGUI, so a screen can move to UI Toolkit later (D-65).
- **A10 Stateless server.** No player state kept in memory between requests; all state in PostgreSQL. Scale = more server instances behind a load balancer (Redis backplane for SignalR when added).
- **A11 Modules.** Server code split by area (Accounts, Champions, Inventory, Campaign, Battles, Summons); modules talk through services, not each other's tables, so one can move to its own service later.

## Data flow
Client action → HTTP+JSON intent (Unity Auth token + request ID) → server verifies token, loads player state → `shared` rule validates & computes → persist + ledger entry (one transaction) → return delta → client updates view. Live PvP later: SignalR.
