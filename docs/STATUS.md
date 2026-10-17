# STATUS
> Read every session. Keep under ~40 lines. Detailed history = git log.

**Phase:** M2a Combat (single fight) · **Next:** M2a-T4 · user installs Docker Desktop (then run DB test) · art: first hero parts-sheet test (ART-7)

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
- [ ] T4 Status effects + cooldowns (CMB-40, BAL-15, CMB-42)
- [ ] T5 Hero kit specials (Fade, self Def down, heal, Ward) + Shift
- [ ] T6 Enemy AI + intents (CMB-41) + determinism test (same seed + inputs = same result, A3)
- [ ] T7 Unity fight screen, placeholder boxes, plays the sim (Unity MCP: ask cost first)
- [ ] T8 Unity feedback (damage numbers, PrimeTween), win/lose, Seedbed 1–6 test-fight picker; user play-check
- M2b later: Herald, doors/Join/Fall back, room options (Q7 room %/door heal, Q8 Margin Strike/Strike Out still to ask), Noise, Notice, Rewrite, Retreat

## Last session
- 2026-10-17: M2a-T2 + T3 done. `shared/Combat`: `Fight` (lines Front-first, `Step`/`Run` + `ActHook`, `Attack`, `Damage` → Down + gap close, win/lose, round cap), `SpeedTurnOrder` behind `ITurnOrder` (BAL-3).
- T3: `DamageCalc` (BAL-2, crit cap, Exposed +25%, spread, rounding), `Targeting` (melee nearest, ranged/Dive any, Splash D-78, Row). 101 tests green.
- D-82 fight-core defaults, D-83 damage roll defaults (Claude defaults). Exposed only triggers once M2b adds rear positions.
- Next: M2a-T4 status effects + cooldowns (CMB-40, BAL-15, CMB-42); FightUnit.Speed/Stats get effect modifiers there.
- User: one question at a time, plain words, small demo, free maintained packages, state Claude usage cost before heavy tools.
