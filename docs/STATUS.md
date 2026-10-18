# STATUS
> Read every session. Keep under ~40 lines. Detailed history = git log.

**Phase:** M2a Combat (single fight) · **Next:** M2b-T6 Notice meter + Rewrite · user installs Docker Desktop (then run DB test) · art: first hero parts-sheet test (ART-7)

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
- [x] T3 Join / Fall back (CMB-10), nearby arrivals (CMB-22), Herald reinforcements
- [x] T4 Room options + Noise (CMB-7, CMB-19): Clash, Margin Strike, Strike Out, Between the Lines, Spin a Tale
- [x] T5 Entry points Margin / Gutter + Bleed-through (CMB-17/18); Exposed starts to trigger (CMB-15)
- [ ] T6 Notice meter (CMB-23) + Rewrite (CMB-20) — design approved (D-106, D-107), ready to code
- [ ] T7 Unity: stage map + scene preview + deploy screens (placeholder)
- [ ] T8 Unity: play the whole Seedbed; user play-check

## Last session
- 2026-10-17: M2a done; M2b-T0..T4 done (stages.json, StageRun, Join/arrivals/Herald, room options + opening; D-91..99). 187 tests.
- M2b-T5: `Fight` is one row (`Row`, `FightUnit.Pos/Spot/Facing`); Margin/Gutter/Bleed-through (`HeroEntry/EnemyEntry.Spot`), melee both sides, Exposed live, flankers walk back; `StageRun.Enter(..., marginHero)`. D-100..105. 202 tests.
- FightScreen still shows only 3 slots per side (a flanker shows in a hero slot); real row view comes with T7.
- Next: M2b-T6 Notice meter (CMB-23) + Rewrite (CMB-20).
- User: one question at a time, plain words, small demo, free maintained packages, state Claude usage cost before heavy tools.
