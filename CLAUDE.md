# Hero Collector (working title) — Claude guide

2D hero-collecting (gacha) RPG, Android, landscape. Unity client + backend. World: Palimpsest (`docs/gdd/00-universe.md`).

## Roles
- **Claude authors** story, lore, champions, skills, base stats, balancing, and all implementation. **User approves.**
- Write new content as **Proposed**; it becomes **Decided** only after user confirms. Present work compactly for review (tables, not essays).

## Session protocol (token budget matters)
1. Start: read `docs/STATUS.md` only. It names the active task and the docs/rule IDs it needs.
2. Load only what the task links. Find rules by ID with grep (e.g. `grep -rn "GEAR-4" docs/gdd`) instead of reading whole files.
3. Never read: `game-design-decisions.md` (original source, superseded by `docs/gdd/`), `Library/`, `Temp/`, `Logs/`, `obj/`, `bin/`, `.unity`/`.prefab`/`.asset` YAML (use Unity MCP / Editor tools instead).
4. End of task: tick it in `STATUS.md`, rewrite "Last session" (≤5 lines), append new decisions to `docs/decisions.md` (1 line each), update `docs/tech/api.md` / `data.md` only if a contract changed. Commit.
5. Open items (`Q-xx` in `docs/open-questions.md`): never invent a final answer. Ask the user, or implement behind a config value and note it.

## Doc index
| File | Read when |
|---|---|
| `docs/STATUS.md` | Every session (current milestone, tasks, blockers) |
| `docs/plan/roadmap.md` | Picking the next milestone / planning |
| `docs/plan/demo.md` | Demo scope: content, roster, systems in/out |
| `docs/decisions.md` | Unsure if something was already decided |
| `docs/open-questions.md` | Hitting an undecided item |
| `docs/gdd/00-universe.md` | Story, lore, champions, enemies, stage themes |
| `docs/gdd/01-overview.md` | Need the big picture / design pillars |
| `docs/gdd/02-combat.md` | Combat work |
| `docs/gdd/03-campaign.md` | Campaign, stages, stars |
| `docs/gdd/04-champions.md` | Rarity, level, rank, skills |
| `docs/gdd/05-awakening.md` | Duplicates, Soul Stones, stars |
| `docs/gdd/06-classes.md` | Classes and class rules |
| `docs/gdd/07-gear.md` | Gear slots, upgrade, merge, dismantle |
| `docs/gdd/08-economy.md` | Resources, currencies, sources/sinks |
| `docs/gdd/09-modes.md` | Dungeons, events, summoning |
| `docs/gdd/10-enemies.md` | Enemy roster, enemy roles, boss candidates |
| `docs/gdd/11-story.md` | Story beats per Volume/chapter, rescues |
| `docs/gdd/balancing.md` | Seeding placeholder numbers |
| `docs/gdd/glossary.md` | Unknown game term |
| `docs/art/style.md` | Making or briefing art (ART-n rules, Gemini prompt) |
| `docs/tech/architecture.md` | Cross-cutting design, client/server split |
| `docs/tech/backend.md` | Backend work |
| `docs/tech/unity.md` | Unity work |
| `docs/tech/data.md` | Content data / player save schema |
| `docs/tech/api.md` | Client↔server calls |

## Conventions
- Doc status tags: **Decided** (final) · **Proposed** (my suggestion, needs user OK) · **Open** (see `Q-xx`).
- GDD rules carry IDs (`AREA-n`). Code comments/tests/commits reference them, e.g. `// GEAR-7`.
- Balancing numbers live in content data (`content/`), not in code and not in GDD prose.
- Server is authoritative for anything that grants or spends value (currencies, summons, rewards).
- Keep docs terse: bullets over prose, no duplication; link by path or ID instead of repeating.
