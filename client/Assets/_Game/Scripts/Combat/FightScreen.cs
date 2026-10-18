#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Core;
using Game.Shared.Combat;
using Game.Shared.Content;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Combat
{
    /// <summary>
    /// M2a-T7/T8 placeholder fight screen: builds its own uGUI from code, plays the shared sim.
    /// Test mode: Seedbed 1–6 test-fight picker (<see cref="TestFight"/>, D-88). Stage mode (M2b-T7):
    /// <see cref="PlayStage"/> plays a scene's fight from <c>StageRun</c> with Join / Fall back (CMB-10) and Retreat (CMB-35).
    /// Row view (D-105): one box per unit in <see cref="Fight.Row"/> order, left (our Main Door) to right, so a
    /// Margin flanker shows behind the enemy Back (CMB-17). Heroes: pick a skill, then a target box
    /// (Self/AllAllies skills fire at once); Shift via the arrow buttons (CMB-16).
    /// Enemies play their intent (CMB-41) after a short delay. Real art: DEMO-12.
    /// </summary>
    public sealed class FightScreen : MonoBehaviour
    {
        [SerializeField] private ulong seed = 1;
        [SerializeField] private float enemyDelay = 0.7f;

        private const int MaxBoxes = Fight.MaxSlots * 2;

        private ContentDb _db = null!;
        private Fight _fight = null!;
        private readonly List<Slot> _slots = new List<Slot>();
        private readonly List<Button> _skillButtons = new List<Button>();
        private readonly List<Button> _joinButtons = new List<Button>();
        private readonly Queue<string> _log = new Queue<string>();
        private Text _header = null!, _logText = null!, _hint = null!;
        private Button _shiftBack = null!, _shiftFront = null!, _fallBack = null!, _retreat = null!;
        private RectTransform _root = null!;
        private GameObject _picker = null!, _result = null!, _testButtons = null!, _stageButtons = null!;
        private Text _resultText = null!;
        private TestFight _setup = TestFight.Seedbed[0];
        private readonly HashSet<FightUnit> _crits = new HashSet<FightUnit>();
        private SkillDef? _picked;
        private bool _busy;

        // Stage mode (M2b-T7)
        private bool _stageMode;
        private string _title = "";
        private int _capacity = Fight.MaxSlots;
        private Action? _onDone, _onRetreat;

        private sealed class Slot
        {
            public Image Box = null!;
            public Outline Border = null!;
            public Text Label = null!;
            public FightUnit? Unit;
        }

        private void Awake() => BuildUi();

        private void Start()
        {
            if (_stageMode) return;
            _db = ContentLoader.Load(new ResourcesContentSource());
            ShowPicker(true);
        }

        /// <summary>
        /// Stage mode: plays this scene's fight. <paramref name="onDone"/> runs from the result panel once the
        /// fight is over; <paramref name="onRetreat"/> from the Retreat button (any time, CMB-35).
        /// </summary>
        public void PlayStage(Fight fight, string title, int capacity, Action onDone, Action onRetreat)
        {
            StopAllCoroutines();
            _stageMode = true;
            _db = fight.Db;
            _fight = fight;
            _title = title;
            _capacity = capacity;
            _onDone = onDone;
            _onRetreat = onRetreat;
            _root.gameObject.SetActive(true);
            _picker.SetActive(false);
            _result.SetActive(false);
            _testButtons.SetActive(false);
            _stageButtons.SetActive(true);
            Begin(title);
        }

        /// <summary>Call right after AddComponent: no test picker, hidden until <see cref="PlayStage"/>.</summary>
        public void UseForStages()
        {
            _stageMode = true;
            Hide();
        }

        public void Hide()
        {
            StopAllCoroutines();
            _root.gameObject.SetActive(false);
        }

        private void ShowPicker(bool show)
        {
            StopAllCoroutines();
            _picker.SetActive(show);
            _result.SetActive(false);
            _testButtons.SetActive(true);
            _stageButtons.SetActive(false);
            if (!show) NewFight();
        }

        // ---------- flow ----------

        private void NewFight()
        {
            _fight = new Fight(_db, new FightSetup
            {
                Heroes = _setup.Heroes.Select(id => new HeroEntry
                    { ChampionId = id, Level = _setup.HeroLevel, Rank = _setup.HeroRank }).ToList(),
                Enemies = _setup.Enemies.Select(id => new EnemyEntry { EnemyId = id, Level = _setup.EnemyLevel }).ToList(),
                Seed = seed,
            });
            _title = _setup.Name;
            Begin($"{_setup.Name}: heroes Lv{_setup.HeroLevel} R{_setup.HeroRank} vs enemies Lv{_setup.EnemyLevel} (seed {seed})");
        }

        private void Begin(string firstLine)
        {
            _log.Clear();
            _picked = null;
            _busy = false;
            Log(firstLine);
            Advance();
        }

        /// <summary>Plays enemy / stunned turns until a hero needs input or the fight ends.</summary>
        private void Advance()
        {
            var next = _fight.Next();
            Refresh();
            if (next == null) return;
            if (next.Side == Side.Enemies || next.Has(StatusEffect.Stun)) StartCoroutine(AutoTurn(next));
        }

        private IEnumerator AutoTurn(FightUnit unit)
        {
            _busy = true;
            Refresh();
            yield return new WaitForSeconds(enemyDelay);
            if (unit.Has(StatusEffect.Stun)) Log($"{unit.Name} is stunned");
            Play((f, u) =>
            {
                var plan = f.IntentOf(u);
                var hits = EnemyAi.Act(f, u);
                LogAction(u, plan?.Skill, hits);
            });
            _busy = false;
            Advance();
        }

        private void HeroAct(FightUnit target)
        {
            var skill = _picked!;
            _picked = null;
            Play((f, u) => LogAction(u, skill, f.UseSkill(u, skill, target)));
            Advance();
        }

        private void HeroShift(int direction)
        {
            var actor = _fight.Next()!;
            var ally = _fight.Line(Side.Heroes)[actor.Line + direction];
            _picked = null;
            Play((f, u) => { f.Shift(u, ally); Log($"{u.Name} shifts with {ally.Name}"); });
            Advance();
        }

        /// <summary>CMB-10 Fall back: the acting champion goes to the door, keeping its HP (costs its turn).</summary>
        private void HeroFallBack()
        {
            _picked = null;
            Play((f, u) => { f.FallBack(u); Log($"{u.Name} falls back to the door"); });
            Advance();
        }

        /// <summary>CMB-10 Join: free, between turns; acts from next round.</summary>
        private void HeroJoin(string championId)
        {
            if (_busy || _fight.IsOver) return;
            try { Log($"{_fight.Join(championId).Name} joins at the Main Door"); }
            catch (Exception e) { Log(e.Message); }
            Refresh();
        }

        /// <summary>One turn, then a floating number for every HP change (hits, heals, Bleed).</summary>
        private void Play(ActHook act)
        {
            var hp = _fight.Units.ToDictionary(u => u, u => u.Hp);
            var where = _slots.Where(sl => sl.Unit != null).ToDictionary(sl => sl.Unit!, sl => sl.Box.rectTransform);
            _crits.Clear();
            _fight.Step(act);
            foreach (var u in _fight.Units)
            {
                int diff = u.Hp - hp[u];
                if (diff != 0 && where.TryGetValue(u, out var at)) SpawnNumber(at, diff, _crits.Contains(u));
            }
        }

        private void SpawnNumber(RectTransform at, int diff, bool crit)
        {
            var t = Ui.Label(_root, "Number", crit ? 54 : 40, TextAnchor.MiddleCenter);
            t.text = diff > 0 ? $"+{diff}" : crit ? $"{-diff}!" : $"{-diff}";
            var color = diff > 0 ? new Color(0.4f, 1f, 0.5f) : crit ? new Color(1f, 0.85f, 0.2f) : Color.white;
            t.color = color;
            var rt = t.rectTransform;
            rt.sizeDelta = new Vector2(240, 80);
            rt.position = at.position;
            var start = rt.anchoredPosition;
            var go = t.gameObject;
            Tween.Custom(0f, 1f, 1f, k =>
                {
                    rt.anchoredPosition = start + new Vector2(0, 120f * k);
                    t.color = new Color(color.r, color.g, color.b, 1f - k * k);
                    rt.localScale = Vector3.one * (k < 0.15f ? 1f + k * 2f : 1.3f - (k - 0.15f) * 0.35f);
                }, Ease.OutQuad)
                .OnComplete(() => Destroy(go));
        }

        private void OnSkill(int index)
        {
            var actor = _fight.Next();
            if (actor == null || _busy) return;
            var skill = actor.Skills[index];
            if (!actor.CanUse(skill)) return;
            if (skill.Area == Area.Self || skill.Area == Area.AllAllies) { _picked = skill; HeroAct(actor); return; }
            _picked = _picked == skill ? null : skill;
            Refresh();
        }

        private void OnSlot(Slot slot)
        {
            var actor = _fight.Next();
            if (_busy || _picked == null || actor == null || slot.Unit == null) return;
            if (!Targeting.Valid(_fight, actor, _picked).Contains(slot.Unit)) return;
            HeroAct(slot.Unit);
        }

        // ---------- view ----------

        private void Refresh()
        {
            var next = _fight.Next();
            var hero = next != null && next.Side == Side.Heroes && !_busy && !next.Has(StatusEffect.Stun) ? next : null;
            var valid = hero != null && _picked != null ? Targeting.Valid(_fight, hero, _picked) : new List<FightUnit>();

            // D-105 row: boxes centred, left (our Main Door end) to right.
            var row = _fight.Row;
            for (int i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i];
                s.Unit = i < row.Count ? row[i] : null;
                s.Box.gameObject.SetActive(s.Unit != null);
                var u = s.Unit;
                if (u == null) continue;
                Ui.Place(s.Box.rectTransform, 0.5f + (i - (row.Count - 1) / 2f) * 0.145f, 0.6f, 250, 330);
                s.Box.color = u.Side == Side.Heroes ? Ui.Hero : Ui.Enemy;
                s.Border.enabled = u == next || valid.Contains(u);
                s.Border.effectColor = u == next ? Ui.Active : Ui.Picked;
                s.Label.text = Describe(u);
            }

            for (int i = 0; i < _skillButtons.Count; i++)
            {
                var b = _skillButtons[i];
                bool show = hero != null && i < hero.Skills.Count;
                b.gameObject.SetActive(show);
                if (!show) continue;
                var sk = hero!.Skills[i];
                int cd = hero.Cooldown(sk.Slot);
                b.interactable = hero.CanUse(sk);
                Ui.SetText(b, $"{sk.Slot} {sk.Name}\n{SkillInfo(sk)}{(cd > 0 ? $"  (cd {cd})" : "")}");
                b.image.color = _picked == sk ? Ui.Picked : Color.white;
            }
            var heroes = _fight.Line(Side.Heroes);
            _shiftFront.gameObject.SetActive(CanShift(hero, -1));
            _shiftBack.gameObject.SetActive(CanShift(hero, +1));
            _fallBack.gameObject.SetActive(_stageMode && hero != null && heroes.Count > 1);

            var bench = _fight.Bench;
            bool canJoin = !_fight.IsOver && !_busy && heroes.Count < _capacity;
            for (int i = 0; i < _joinButtons.Count; i++)
            {
                var b = _joinButtons[i];
                bool show = canJoin && i < bench.Count;
                b.gameObject.SetActive(show);
                if (!show) continue;
                var e = bench[i];
                var def = _db.Champion(e.ChampionId);
                Ui.SetText(b, $"Join: {def.Name}{(e.Hp is int hp ? $" (HP {hp})" : "")}");
            }
            _retreat.gameObject.SetActive(_stageMode && !_fight.IsOver);

            string incoming = _fight.IncomingCount > 0 ? $" · {_fight.IncomingCount} incoming" : "";
            _header.text = _fight.IsOver
                ? $"{_fight.Outcome}{(_fight.LossReason != LossReason.None ? $" ({_fight.LossReason})" : "")} - round {_fight.Round}"
                : $"{(_fight.InOpening ? "Opening" : $"Round {_fight.Round}/{_fight.RoundCap}")} · {(next != null ? next.Name : "")}'s turn{incoming}";
            _hint.text = _fight.IsOver ? "" : hero == null ? "Enemy turn..." : _picked == null ? "Pick a skill" : "Pick a green target";
            _result.SetActive(_fight.IsOver);
            if (_fight.IsOver)
                _resultText.text = _fight.Outcome == FightOutcome.Won
                    ? $"Victory!\n<size=28>{_title}, round {_fight.Round}</size>"
                    : $"Defeat\n<size=28>{(_fight.LossReason == LossReason.RoundCap ? $"round cap {_fight.RoundCap} reached" : "all heroes Down")}</size>";
            _logText.text = string.Join("\n", _log);
        }

        /// <summary>CMB-16: only Main-spot champions next to each other swap; flankers don't.</summary>
        private bool CanShift(FightUnit? hero, int direction)
        {
            if (hero == null || hero.Spot != Spot.Main) return false;
            var line = _fight.Line(Side.Heroes);
            int at = hero.Line + direction;
            return at >= 0 && at < line.Count && line[at].Spot == Spot.Main;
        }

        private string Describe(FightUnit u)
        {
            var sb = new StringBuilder();
            sb.Append($"<b>{u.Name}</b> {(u.Facing > 0 ? ">>" : "<<")}");
            if (u.Spot == Spot.Behind) sb.Append(u.Side == Side.Heroes ? "\n<color=#9fe8ff>Margin flanker</color>" : "\n<color=#ffb0b0>Bleed-through</color>");
            else if (u.Spot == Spot.Gutter) sb.Append("\n<color=#ffb0b0>Gutter</color>");
            sb.Append($"\nHP {u.Hp}/{u.MaxHp}\nAtk {u.Attack} Def {u.Defense} Spd {u.Speed}");
            foreach (var e in u.Effects)
                sb.Append($"\n{e.Type} {e.Rounds}{(e.Type == StatusEffect.Ward ? $" ({e.Amount})" : "")}");
            if (u.IsFaded) sb.Append("\nFaded");
            if (u.RingIn > 0) sb.Append($"\nRings in {u.RingIn}");
            var intent = _fight.IntentOf(u);
            if (intent != null) sb.Append($"\n<color=#ffd34d>Next: {intent.Skill.Name} > {intent.Target.Name}</color>");
            return sb.ToString();
        }

        private static string SkillInfo(SkillDef s)
        {
            var parts = new List<string>();
            if (s.DamagePercent > 0) parts.Add($"{s.DamagePercent}% {s.Reach}{(s.Dive ? " Dive" : "")} {s.Area}");
            else parts.Add(s.Area.ToString());
            if (s.HealPercent > 0) parts.Add($"heal {s.HealPercent}%");
            if (s.WardPercent > 0) parts.Add($"ward {s.WardPercent}%");
            parts.AddRange(s.Effects.Select(e => $"{e.Type} {e.Rounds}"));
            if (s.Special != null) parts.Add(s.Special.Type.ToString());
            return string.Join(", ", parts);
        }

        private void LogAction(FightUnit u, SkillDef? skill, IReadOnlyList<Hit> hits)
        {
            if (skill == null) { Log($"{u.Name} has no target"); return; }
            foreach (var h in hits) if (h.Crit) _crits.Add(h.Target);
            var text = hits.Count == 0 ? "" : ": " + string.Join(", ",
                hits.Select(h => $"{h.Target.Name} -{h.HpLost}{(h.Crit ? " CRIT" : "")}{(h.Target.IsDown ? " DOWN" : "")}"));
            Log($"{u.Name} uses {skill.Name}{text}");
        }

        private void Log(string line)
        {
            _log.Enqueue(line);
            while (_log.Count > 9) _log.Dequeue();
        }

        // ---------- UI built from code (no prefabs yet) ----------

        private void BuildUi()
        {
            var root = _root = Ui.Canvas(transform, "FightCanvas", 10);

            var bg = Ui.Panel(root, "Background", new Color(0.12f, 0.11f, 0.1f));
            Ui.Stretch(bg.rectTransform, 0, 0, 1, 1);

            _header = Ui.Label(root, "Header", 40, TextAnchor.MiddleCenter);
            Ui.Place(_header.rectTransform, 0.5f, 0.95f, 1400, 70);
            _hint = Ui.Label(root, "Hint", 28, TextAnchor.MiddleCenter);
            Ui.Place(_hint.rectTransform, 0.5f, 0.89f, 1000, 50);

            for (int i = 0; i < MaxBoxes; i++)
            {
                var box = Ui.Panel(root, $"Box{i}", Ui.Empty);
                var slot = new Slot { Box = box };
                slot.Border = box.gameObject.AddComponent<Outline>();
                slot.Border.effectDistance = new Vector2(6, 6);
                slot.Label = Ui.Label(box.rectTransform, "Info", 22, TextAnchor.UpperCenter);
                Ui.Stretch(slot.Label.rectTransform, 0.04f, 0.03f, 0.96f, 0.97f);
                box.gameObject.AddComponent<Button>().onClick.AddListener(() => OnSlot(slot));
                _slots.Add(slot);
            }

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var b = Ui.Button(root, $"Skill{i}", 22, () => OnSkill(index));
                Ui.Place(Ui.Rect(b), 0.22f + i * 0.18f, 0.2f, 340, 110); // clear of the log (right)
                _skillButtons.Add(b);
            }
            _shiftBack = Ui.Button(root, "ShiftBack", 24, () => HeroShift(+1), "< Shift back");
            Ui.Place(Ui.Rect(_shiftBack), 0.07f, 0.2f, 200, 80);
            _shiftFront = Ui.Button(root, "ShiftFront", 24, () => HeroShift(-1), "Shift front >");
            Ui.Place(Ui.Rect(_shiftFront), 0.07f, 0.11f, 200, 80);
            _fallBack = Ui.Button(root, "FallBack", 24, HeroFallBack, "Fall back");
            Ui.Place(Ui.Rect(_fallBack), 0.07f, 0.29f, 200, 70);
            for (int i = 0; i < Fight.MaxSlots; i++)
            {
                int index = i;
                var b = Ui.Button(root, $"Join{i}", 22, () => HeroJoin(_fight.Bench[index].ChampionId));
                Ui.Place(Ui.Rect(b), 0.1f + i * 0.14f, 0.36f, 250, 60);
                _joinButtons.Add(b);
            }
            _retreat = Ui.Button(root, "Retreat", 24, () => _onRetreat?.Invoke(), "Retreat");
            Ui.Place(Ui.Rect(_retreat), 0.94f, 0.95f, 160, 60);

            _logText = Ui.Label(root, "Log", 20, TextAnchor.LowerLeft);
            Ui.Place(_logText.rectTransform, 0.855f, 0.17f, 520, 280);

            // Win / lose panel (on top of the row).
            var result = Ui.Panel(root, "Result", new Color(0, 0, 0, 0.8f));
            Ui.Place(result.rectTransform, 0.5f, 0.55f, 760, 420);
            _result = result.gameObject;
            _resultText = Ui.Label(result.rectTransform, "Text", 64, TextAnchor.MiddleCenter);
            Ui.Place(_resultText.rectTransform, 0.5f, 0.7f, 700, 200);
            var testButtons = Ui.Panel(result.rectTransform, "TestButtons", Color.clear);
            Ui.Stretch(testButtons.rectTransform, 0, 0, 1, 1);
            _testButtons = testButtons.gameObject;
            AddButton(testButtons.rectTransform, "Retry (same seed)", 0.2f, 0.22f, () => ShowPicker(false));
            AddButton(testButtons.rectTransform, "Next seed", 0.5f, 0.22f, () => { seed++; ShowPicker(false); });
            AddButton(testButtons.rectTransform, "Pick fight", 0.8f, 0.22f, () => ShowPicker(true));
            var stageButtons = Ui.Panel(result.rectTransform, "StageButtons", Color.clear);
            Ui.Stretch(stageButtons.rectTransform, 0, 0, 1, 1);
            _stageButtons = stageButtons.gameObject;
            AddButton(stageButtons.rectTransform, "Continue", 0.5f, 0.22f, () => _onDone?.Invoke());

            // Test-fight picker (Seedbed 1–6, D-88).
            var picker = Ui.Panel(root, "Picker", new Color(0.08f, 0.08f, 0.07f, 0.97f));
            Ui.Stretch(picker.rectTransform, 0, 0, 1, 1);
            _picker = picker.gameObject;
            var title = Ui.Label(picker.rectTransform, "Title", 48, TextAnchor.MiddleCenter);
            title.text = "Test fight";
            Ui.Place(title.rectTransform, 0.5f, 0.85f, 800, 90);
            for (int i = 0; i < TestFight.Seedbed.Length; i++)
            {
                var tf = TestFight.Seedbed[i];
                var b = AddButton(picker.rectTransform, $"{tf.Name}\nLv{tf.EnemyLevel}: {string.Join(" ", tf.Enemies)}",
                    0.25f + (i % 3) * 0.25f, i < 3 ? 0.6f : 0.35f, () => { _setup = tf; ShowPicker(false); });
                Ui.Rect(b).sizeDelta = new Vector2(400, 150);
            }
            _picker.SetActive(false);
            _result.SetActive(false);
        }

        private static Button AddButton(RectTransform parent, string text, float ax, float ay, UnityEngine.Events.UnityAction onClick)
        {
            var b = Ui.Button(parent, text, 26, onClick, text);
            Ui.Place(Ui.Rect(b), ax, ay, 220, 80);
            return b;
        }
    }
}
