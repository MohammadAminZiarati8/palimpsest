# STATUS
> Read every session. Keep under ~40 lines. Detailed history = git log.

**Phase:** M0 setup (GDD for demo done) · **Next:** 3D models vs 2D sprites (style D-68 needs it; may change D-64 "2D URP") → art pipeline Q-23 → M0-4

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
- [ ] M0-4 `server/` skeleton per `tech/backend.md`
- [ ] M0-5 `client/` Unity project (Android, landscape, uGUI); asmdefs + folders per `tech/unity.md`
- [ ] M0-6 `scripts/`: copy `shared` DLL + `content/` into Unity (see `tech/unity.md`)
- [x] M0-7 Update `tech/*.md` from Proposed → Decided for chosen stack

## Last session
- 2026-10-17: Q-04 → uGUI (D-65). Architecture A1–A11 approved (D-66): added safe retries, ledger, content version, client layers, stateless server, modules.
- M0-7, M0-1 (repo on GitHub), M0-2 (Claude read-deny rules), M0-3 (shared lib + SeededRandom, A3) done.
- Art style decided (D-68, `art/style.md`): AC Rebellion-like stylized 3D look; real art in demo.
- User rule: free, actively maintained, community-recommended packages; no cloud CI (local scripts).
- User prefers questions one at a time, plain words, small demo scope. Repo on GitHub (M0-1); commit identity set per-repo (MohammadAminZiarati8).
