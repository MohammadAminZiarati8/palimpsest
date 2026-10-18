#!/bin/sh
# usage: scene.sh "Hero0 Hero1" [OptionX] -> deploy, pick option, enter, auto-fight, continue; prints after text.
D=$(dirname "$0")
for b in $1; do sh $D/click.sh $b | grep -o '"result": "[^"]*"'; done
[ -n "$2" ] && sh $D/click.sh $2 | grep -o '"result": "[^"]*"'
sh $D/click.sh Enter | grep -o '"result": "[^"]*"'
sleep 1
sh $D/fight.sh 80 "$3"
sh $D/click.sh Continue | grep -o '"result": "[^"]*"'
sleep 0.5
unity command eval --code 'var t = System.Linq.Enumerable.FirstOrDefault(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(UnityEngine.FindObjectsSortMode.None), x => x.name == "Text" && x.transform.parent.name == "Box" && x.gameObject.activeInHierarchy); return t == null ? "no after panel" : System.Text.RegularExpressions.Regex.Replace(t.text, "<[^>]+>", "");' --no-banner --caller plugin --skill unity-cli --result-only 2>&1 | python -I -c "import sys,json; d=json.load(sys.stdin); print('AFTER:', d.get('result') or d)"
