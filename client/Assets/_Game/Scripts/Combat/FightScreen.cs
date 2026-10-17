#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Core;
using Game.Shared.Combat;
using Game.Shared.Content;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Combat
{
    /// <summary>
    /// M2a-T7 placeholder fight screen: builds its own uGUI from code, plays the shared sim.
    /// Row (CMB-11): ours [Back][Middle][Front] vs [Front][Middle][Back] enemies. Heroes: pick a skill,
    /// then a target box (Self/AllAllies skills fire at once); Shift via the arrow buttons (CMB-16).
    /// Enemies play their intent (CMB-41) after a short delay. Real art/feedback: T8.
    /// </summary>
    public sealed class FightScreen : MonoBehaviour
    {
        [SerializeField] private string[] heroes = { "c01", "c02", "c04" };
        [SerializeField] private string[] enemies = { "C1", "C2", "C4" };
        [SerializeField] private int heroLevel = 1;
        [SerializeField] private int enemyLevel = 1;
        [SerializeField] private ulong seed = 1;
        [SerializeField] private float enemyDelay = 0.7f;

        private static readonly Color HeroColor = new Color(0.25f, 0.45f, 0.8f);
        private static readonly Color EnemyColor = new Color(0.75f, 0.3f, 0.3f);
        private static readonly Color EmptyColor = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color ActiveColor = new Color(1f, 0.85f, 0.2f);
        private static readonly Color TargetColor = new Color(0.3f, 1f, 0.4f);

        private ContentDb _db = null!;
        private Fight _fight = null!;
        private Font _font = null!;
        private readonly List<Slot> _slots = new List<Slot>();
        private readonly List<Button> _skillButtons = new List<Button>();
        private readonly Queue<string> _log = new Queue<string>();
        private Text _header = null!, _logText = null!, _hint = null!;
        private Button _shiftBack = null!, _shiftFront = null!, _restart = null!;
        private SkillDef? _picked;
        private bool _busy;

        private sealed class Slot
        {
            public Side Side;
            public int Line;
            public Image Box = null!;
            public Outline Border = null!;
            public Text Label = null!;
            public FightUnit? Unit;
        }

        private void Awake()
        {
            _db = ContentLoader.Load(new ResourcesContentSource());
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUi();
            NewFight();
        }

        // ---------- flow ----------

        private void NewFight()
        {
            StopAllCoroutines();
            _fight = new Fight(_db, new FightSetup
            {
                Heroes = heroes.Select(id => new HeroEntry { ChampionId = id, Level = heroLevel }).ToList(),
                Enemies = enemies.Select(id => new EnemyEntry { EnemyId = id, Level = enemyLevel }).ToList(),
                Seed = seed,
            });
            _log.Clear();
            _picked = null;
            _busy = false;
            Log($"Fight start (seed {seed})");
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
            _fight.Step((f, u) =>
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
            var actor = _fight.Next()!;
            _fight.Step((f, u) => LogAction(u, skill, f.UseSkill(u, skill, target)));
            Advance();
        }

        private void HeroShift(int direction)
        {
            var actor = _fight.Next()!;
            var line = _fight.Line(Side.Heroes);
            var ally = line[actor.Line + direction];
            _picked = null;
            _fight.Step((f, u) => { f.Shift(u, ally); Log($"{u.Name} shifts with {ally.Name}"); });
            Advance();
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

            foreach (var s in _slots)
            {
                var line = _fight.Line(s.Side);
                s.Unit = s.Line < line.Count ? line[s.Line] : null;
                var u = s.Unit;
                s.Box.color = u == null ? EmptyColor : s.Side == Side.Heroes ? HeroColor : EnemyColor;
                s.Border.enabled = u != null && (u == next || valid.Contains(u));
                s.Border.effectColor = u == next ? ActiveColor : TargetColor;
                s.Label.text = u == null ? "" : Describe(u);
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
                b.GetComponentInChildren<Text>().text =
                    $"{sk.Slot} {sk.Name}\n{SkillInfo(sk)}{(cd > 0 ? $"  (cd {cd})" : "")}";
                b.image.color = _picked == sk ? TargetColor : Color.white;
            }
            var heroes = _fight.Line(Side.Heroes);
            _shiftFront.gameObject.SetActive(hero != null && hero.Line > 0);
            _shiftBack.gameObject.SetActive(hero != null && hero.Line < heroes.Count - 1);

            _header.text = _fight.IsOver
                ? $"{_fight.Outcome}{(_fight.LossReason != LossReason.None ? $" ({_fight.LossReason})" : "")} - round {_fight.Round}"
                : $"Round {_fight.Round}/{_fight.RoundCap} · {(next != null ? next.Name : "")}'s turn";
            _hint.text = _fight.IsOver ? "" : hero == null ? "Enemy turn..." : _picked == null ? "Pick a skill" : "Pick a green target";
            _restart.gameObject.SetActive(_fight.IsOver);
            _logText.text = string.Join("\n", _log);
        }

        private string Describe(FightUnit u)
        {
            var sb = new StringBuilder();
            sb.Append($"<b>{u.Name}</b>\nHP {u.Hp}/{u.MaxHp}\nAtk {u.Attack} Def {u.Defense} Spd {u.Speed}");
            foreach (var e in u.Effects)
                sb.Append($"\n{e.Type} {e.Rounds}{(e.Type == StatusEffect.Ward ? $" ({e.Amount})" : "")}");
            if (u.IsFaded) sb.Append("\nFaded");
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
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("FightCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)canvasGo.transform;

            var bg = Panel(root, "Background", new Color(0.12f, 0.11f, 0.1f));
            Stretch(bg.rectTransform, 0, 0, 1, 1);

            _header = Label(root, "Header", 40, TextAnchor.MiddleCenter);
            Place(_header.rectTransform, 0.5f, 0.95f, 1400, 70);
            _hint = Label(root, "Hint", 28, TextAnchor.MiddleCenter);
            Place(_hint.rectTransform, 0.5f, 0.89f, 1000, 50);

            // Row: hero Back(2) Middle(1) Front(0) | enemy Front(0) Middle(1) Back(2)
            float[] xs = { 0.09f, 0.23f, 0.37f, 0.63f, 0.77f, 0.91f };
            for (int i = 0; i < 6; i++)
            {
                var side = i < 3 ? Side.Heroes : Side.Enemies;
                int line = i < 3 ? 2 - i : i - 3;
                var box = Panel(root, $"{side}{line}", EmptyColor);
                Place(box.rectTransform, xs[i], 0.6f, 250, 330);
                var slot = new Slot { Side = side, Line = line, Box = box };
                slot.Border = box.gameObject.AddComponent<Outline>();
                slot.Border.effectDistance = new Vector2(6, 6);
                slot.Label = Label(box.rectTransform, "Info", 22, TextAnchor.UpperCenter);
                Stretch(slot.Label.rectTransform, 0.04f, 0.03f, 0.96f, 0.97f);
                box.gameObject.AddComponent<Button>().onClick.AddListener(() => OnSlot(slot));
                _slots.Add(slot);
            }
            var vs = Label(root, "Versus", 48, TextAnchor.MiddleCenter);
            vs.text = "VS";
            Place(vs.rectTransform, 0.5f, 0.6f, 120, 80);

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var b = MakeButton(root, $"Skill{i}", 22, () => OnSkill(index));
                Place((RectTransform)b.transform, 0.3f + i * 0.2f, 0.2f, 360, 110);
                _skillButtons.Add(b);
            }
            _shiftBack = MakeButton(root, "ShiftBack", 24, () => HeroShift(+1));
            _shiftBack.GetComponentInChildren<Text>().text = "< Shift back";
            Place((RectTransform)_shiftBack.transform, 0.07f, 0.2f, 200, 80);
            _shiftFront = MakeButton(root, "ShiftFront", 24, () => HeroShift(-1));
            _shiftFront.GetComponentInChildren<Text>().text = "Shift front >";
            Place((RectTransform)_shiftFront.transform, 0.07f, 0.11f, 200, 80);

            _restart = MakeButton(root, "Restart", 30, () => { seed++; NewFight(); });
            _restart.GetComponentInChildren<Text>().text = "New fight (next seed)";
            Place((RectTransform)_restart.transform, 0.5f, 0.82f, 420, 70);

            _logText = Label(root, "Log", 20, TextAnchor.LowerLeft);
            Place(_logText.rectTransform, 0.83f, 0.17f, 620, 280);
        }

        private static Image Panel(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private Text Label(RectTransform parent, string name, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private Button MakeButton(RectTransform parent, string name, int size, UnityEngine.Events.UnityAction onClick)
        {
            var img = Panel(parent, name, Color.white);
            var b = img.gameObject.AddComponent<Button>();
            b.onClick.AddListener(onClick);
            var t = Label(img.rectTransform, "Text", size, TextAnchor.MiddleCenter);
            t.color = Color.black;
            Stretch(t.rectTransform, 0.03f, 0.05f, 0.97f, 0.95f);
            return b;
        }

        private static void Place(RectTransform rt, float ax, float ay, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
