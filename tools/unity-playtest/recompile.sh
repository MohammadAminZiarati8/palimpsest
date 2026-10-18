#!/bin/sh
# Recompile scripts while unfocused, wait, report console errors, then open Play Stages again.
U="unity command --no-banner --caller plugin --skill unity-cli"
$U editor_stop --result-only >/dev/null 2>&1
$U recompile --result-only >/dev/null 2>&1
for i in $(seq 1 60); do
  s=$($U recompile_status --result-only 2>&1 | tr -d '\n')
  case "$s" in *completed*|*up_to_date*|*idle*) break;; esac
  sleep 3
done
sleep 5
until unity status --format json --no-banner 2>/dev/null | grep -q '"ready"'; do sleep 3; done
echo "recompile: $(echo $s | grep -oE 'completed|up_to_date|idle' | head -1)"
$U console_status --result-only 2>&1 | grep -E 'compilationFailed|consoleErrors'
$U menu --path "Game/Play Stages" --result-only >/dev/null 2>&1
sleep 10
$U eval --code 'UnityEngine.Application.runInBackground = true; UnityEngine.Time.timeScale = 4; return "play " + UnityEditor.EditorApplication.isPlaying;' --result-only 2>&1 | grep result
