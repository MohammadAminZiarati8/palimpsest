# STATUS
> Read every session. Keep under ~40 lines. Detailed history = git log.

**Phase:** M1 done → M2 Combat · **Next:** M2 (combat spec check, deterministic sim in `shared`) · user installs Docker Desktop (then run DB test) · art: first hero parts-sheet test (ART-7)

## GDD tasks (Claude drafts, user approves — D-08)
- [x] Universe (D-07, D-09) · Combat v2 (D-10, D-11) · Business & platform (D-12, D-13)
- [x] GDD-1 Enemy roster: Censor types, enemy roles, boss candidates (Q-25) → `gdd/10-enemies.md`
- [x] GDD-2 Story outline: realms, chapters, rescues → `gdd/11-story.md` (characters Q-26, bosses Q-25 open)
- [x] Mage added as 6th class (D-24, D-34..37)
- [x] GDD-3 Demo definition: Prologue + Seedbed, 4 heroes, online, no auto-battle → `plan/demo.md`
- [x] GDD-4 Demo champions: Margo (Warrior, main), Pell (Assassin), Libby (Mage), Lady White Snake (Support) → `gdd/champions/c01..c04`
- [x] GDD-5 Demo progression numbers → `gdd/balancing.md` BAL-1..19 (D-51..61). Q-07 waits (D-30)
- [x] Tech stack Q-03 → D-62..64 (Unity 6 LTS, shared C#, ASP.NET Core .NET 10, PostgreSQL, Unity Auth, local scripts)
- [x] Q-04 UI system → uGUI (D-65)
- [x] Architecture A1–A11 approved (D-66)
- Deferred until after the demo (D-13): launch scope Q-20, launch roster, currencies Q-13, summoning details Q-16

## Later GDD (not needed for demo)
Campaign rewards/stars Q-05, Q-06 · Dungeons Q-14 · Gear Q-11, Q-12 · Awakening Q-09, Q-10 · Refund Q-08 · Meta Q-22 · Languages Q-21 · Production Q-23 · Title Q-24

## M0 tasks (after GDD)
- [x] M0-1 Git repo + `.gitignore`/`.gitattributes`, pushed to github.com/MohammadAminZiarati8/palimpsest (private)
- [x] M0-2 `.claude/settings.json`: deny reads of Library/Temp/Logs/obj/bin (+ builds, `game-design-decisions.md`)
- [x] M0-3 `shared/` (Game.Shared, netstandard2.1, C# 9) + `shared.Tests/` (xUnit v3, MTP runner via `global.json`); `dotnet test` green
- [x] M0-4 `server/` skeleton per `tech/backend.md` (DB health test skips until Docker Desktop installed)
- [x] M0-5 `client/` Unity 6.3 LTS project (Android, landscape, IL2CPP ARM64, uGUI); asmdefs + folders per `tech/unity.md`
- [x] M0-6 `dotnet build` copies `shared` DLL + `content/` into Unity (see `tech/unity.md`)
- [x] M0-7 Update `tech/*.md` from Proposed → Decided for chosen stack

## M1 (done)
- [x] Content JSON (`version`, `stats`, `classes`, `champions`) + loader with validation; stat calc (BAL-5..9) + level/XP caps (CHM-7, BAL-10/11); 39 shared tests; Unity loads content via Resources

## Last session
- 2026-10-17: uGUI (D-65), architecture A1–A11 (D-66), art style AC Rebellion-like + cut-out 2D sprites + AI art pipeline (D-68..70, `art/style.md`).
- M0 done (repo github.com/MohammadAminZiarati8/palimpsest, server skeleton, Unity 6.3 client, auto-copy shared DLL + content). M1 done (content JSON, loader, stat calc, 41 tests).
- Open for user: install Docker Desktop (then `dotnet test` runs the DB health test). Art: Margo parts-sheet test (ART-7) not tried yet.
- Next: M2 combat — read `gdd/02-combat.md` by rule ID; sim in `shared` with `SeededRandom` (A3); Unity prototype vs test enemies (BAL-16..18).
- User: one question at a time, plain words, small demo, free maintained packages, no cloud CI, state Claude usage cost before heavy tools.
