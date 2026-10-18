#!/bin/sh
# usage: fight.sh [maxTurns] -> auto-plays hero turns (highest usable skill, lowest-HP valid target)
# until the fight is over; prints the final header + log. T8 play-check helper.
max=${1:-60}; avoid=${2:-none}
step0='var fs = UnityEngine.Object.FindAnyObjectByType<Game.Combat.FightScreen>();
if (fs == null) return "nofs";
var T = typeof(Game.Combat.FightScreen); var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var fight = (Game.Shared.Combat.Fight)T.GetField("_fight", BF).GetValue(fs);
var root = (UnityEngine.RectTransform)T.GetField("_root", BF).GetValue(fs);
if (fight == null || !root.gameObject.activeInHierarchy) return "nofight";
if (fight.IsOver) return "over";
if (fight.Line(Game.Shared.Combat.Side.Heroes).Count == 0 && fight.Bench.Count > 0) return "empty";
if ((bool)T.GetField("_busy", BF).GetValue(fs)) return "busy";
var hero = fight.Next();
if (hero == null || hero.Side != Game.Shared.Combat.Side.Heroes) return "busy";
for (int i = hero.Skills.Count - 1; i >= 0; i--)
{
  var sk = hero.Skills[i];
  if (!hero.CanUse(sk)) continue;
  var valid = Game.Shared.Combat.Targeting.Valid(fight, hero, sk);
  T.GetMethod("OnSkill", BF).Invoke(fs, new object[] { i });
  if (T.GetField("_picked", BF).GetValue(fs) == null) return "acted " + hero.Name + " " + sk.Name;
  if (valid.Count == 0) { T.GetField("_picked", BF).SetValue(fs, null); continue; }
  var target = System.Linq.Enumerable.First(System.Linq.Enumerable.OrderBy(valid, u => (u.Name == "AVOID" ? 1000000 : 0) + u.Hp));
  T.GetMethod("HeroAct", BF).Invoke(fs, new object[] { target });
  return "acted " + hero.Name + " " + sk.Name + " > " + target.Name;
}
return "stuck " + hero.Name;'
step=$(printf "%s" "$step0" | sed "s/AVOID/$avoid/")
n=0
while [ $n -lt $max ]; do
  r=$(unity command eval --code "$step" --no-banner --caller plugin --skill unity-cli --result-only 2>&1 | grep '"result"' | sed 's/.*"result": "\(.*\)".*/\1/')
  case "$r" in
    over|nofight|nofs|empty) echo "state: $r"; break;;
    busy) sleep 0.5;;
    stuck*|"") echo "!! $r"; break;;
    *) n=$((n+1));;
  esac
done
unity command eval --code 'var fs = UnityEngine.Object.FindAnyObjectByType<Game.Combat.FightScreen>(); var T = typeof(Game.Combat.FightScreen); var BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var f = (Game.Shared.Combat.Fight)T.GetField("_fight", BF).GetValue(fs);
var sb = new System.Text.StringBuilder(((UnityEngine.UI.Text)T.GetField("_header", BF).GetValue(fs)).text + " | hero turns '"$n"'\n");
foreach (var u in f.Units) sb.Append(u.Name + " " + u.Hp + "/" + u.MaxHp + (u.IsDown ? " DOWN" : "") + "; ");
sb.Append("\n" + ((UnityEngine.UI.Text)T.GetField("_logText", BF).GetValue(fs)).text);
return sb.ToString();' --no-banner --caller plugin --skill unity-cli --result-only 2>&1 | python -I -c "import sys,json; d=json.load(sys.stdin); print(d.get('result') or d)"
