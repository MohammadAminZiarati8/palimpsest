# STATUS
> Read every session. Keep under ~40 lines. Detailed history = git log.

**Phase:** M2a Combat (single fight) · **Next:** M2b-T3 Join / arrivals / Herald · user installs Docker Desktop (then run DB test) · art: first hero parts-sheet test (ART-7)

## Done (details: git log, `decisions.md`)
- GDD for demo (universe, combat v2, enemies, story, demo scope, 4 champions, BAL-1..20); stack D-62..66; art D-68..70
- M0 foundations (repo, shared, server, Unity 6.3 client, DLL/content copy) · M1 content + stat calc (41 tests) · M2a-T1 content (68 tests)
- Deferred until after the demo (D-13): Q-20 launch scope, roster, Q-13 currencies, Q-16 summoning. Later GDD: Q-05/06, Q-08..12, Q-14, Q-21..24

## M2a — single fight (D-73). One fresh session + commit per task; read only what the task names
Rules: CMB-12..15, 25..27, 29, 31, 40..43 · BAL-2..4, 15..18, 20. Code in `shared/Combat`, tests in `shared.Tests`.
- [x] T0 Docs: M2 questions answered (D-73..80)
- [x] T1 Content: hero skills/effects (BAL skill table) + enemies C1/C2/C4/C5 (BAL-16..18) + loader checks
- [x] T2 Fight core: slots row, Speed order (swappable, BAL-3), rounds, gap close, win/lose, round cap
- [x] T3 Damage + targeting: BAL-2, crit, spread, rounding, Exposed (BAL-20), melee/ranged/Dive, Splash/Row; test BAL-2 table + "Inkslinger hit ≈ 10% Pell HP"
- [x] T4 Status effects + cooldowns (CMB-40, BAL-15, CMB-42)
- [x] T5 Hero kit specials (Fade, self Def down, heal, Ward) + Shift
- [x] T6 Enemy AI + intents (CMB-41) + determinism test (same seed + inputs = same result, A3)
- [x] T7 Unity fight screen, placeholder boxes, plays the sim (Unity MCP: ask cost first)
- [x] T8 Unity feedback (damage numbers, PrimeTween), win/lose, Seedbed 1–6 test-fight picker; user play-check

## M2b — stage flow (task split approved, D-90)
Rules: CMB-3..10, 17..23, 33..36 · BAL-21..24 (D-91..94). Code in `shared/Stages`, tests in `shared.Tests`.
- [x] T0 Docs: ask Q7 (room option %, door heal) + Q8 (Margin Strike, Strike Out numbers), one at a time
- [x] T1 Content: `stages.json` (Prologue 1–2, Seedbed 1–6: scenes, capacity, enemies, nearby arrivals, Herald) + loader checks
- [x] T2 Stage sim: scenes in order, party pick per scene (CMB-3/8), HP carries, door heal, Down for stage, fail, Retreat, stars (CMB-33..36)
- [ ] T3 Join / Fall back (CMB-10), nearby arrivals (CMB-22), Herald reinforcements
- [ ] T4 Room options + Noise (CMB-7, CMB-19): Clash, Margin Strike, Strike Out, Between the Lines, Spin a Tale
- [ ] T5 Entry points Margin / Gutter + Bleed-through (CMB-17/18); Exposed starts to trigger (CMB-15)
- [ ] T6 Notice meter (CMB-23) + Rewrite (CMB-20)
- [ ] T7 Unity: stage map + scene preview + deploy screens (placeholder)
- [ ] T8 Unity: play the whole Seedbed; user play-check

## Last session
- 2026-10-17: M2a done (T2..T8, one session): sim in `shared/Combat`, Unity *Game/Test Fight* with Seedbed 1–6 picker (D-88), user play-checked.
- M2b-T0: D-91 door heal 15% (BAL-21), D-92 room option % (BAL-22), D-93 Margin Strike (BAL-23), D-94 Strike Out (BAL-24). D-89 Corrector may self-heal.
- M2b-T1: `content/stages.json` (P1–P2, S1–S6, D-95) + `StageModels.cs` + loader checks; C3 Herald in `enemies.json` (Ring = SpecialType, no effect until T3). 150 tests green.
- M2b-T2: `shared/Stages/StageRun` (scenes in order, party per scene, door heal, HP carry, Down for stage, Retreat, 3 stars); D-96 defaults. 160 tests.
- Next: M2b-T3 Join / Fall back, nearby arrivals, Herald Ring.
- User: one question at a time, plain words, small demo, free maintained packages, state Claude usage cost before heavy tools.
