# Roadmap
> Read when: picking the next milestone. Only the active milestone gets a detailed task list (in `STATUS.md`). Finished milestones collapse to one line here.

| M | Goal | Done when | Blocked by | Status |
|---|---|---|---|---|
| M0 | Foundations | Repo + stack decided; skeletons for shared/server/client build; `dotnet test` runs | — (Q-03, Q-04 decided: D-62..65) | Done (DB test waits for Docker) |
| M1 | Shared core | Content models + JSON loaders; champion stat calc (rarity, level, rank, class growth) tested | — | Done |
| M2 | Combat | Combat spec Decided; deterministic sim in `shared`; playable Unity prototype vs test enemies. Split (D-73): M2a single fight, M2b stage flow | — (Q-02 settled, D-80) | Done (M2a + M2b, 2026-10-19) |
| M3 | Backend core + champion progression | Guest auth, player state, inventory/currencies, ledger; level/rank/skills end-to-end Unity↔server | Q-07, Q-08, Q-13 | Todo |
| M4 | Campaign | Chapters/stages, star results + conditions, rewards, server-verified results | Q-05, Q-06 | Todo |
| M5 | Gear | Equip, level, merge, Forge Stone, dismantle refund, legendary passives | Q-11, Q-12 | Todo |
| M6 | Summoning + Awakening | Banners/rates/pity server-side; Soul Stones, stars, awakened skills, exchange | Q-09, Q-10, Q-16 | Todo |
| M7 | Dungeons | XP / rank-material / gear dungeons with targeted drops | Q-14 | Todo |
| M8 | Events, shop, polish | Event framework, shops, UX pass | Q-15, Q-17 | Todo |

**Demo checkpoint (D-13):** demo = M0–M2 + a slice of M3/M4 (one Volume's first chapters, small demo roster, level/rank). After it: decide launch scope Q-20, launch roster, currencies Q-13.

## Completed
(none)
