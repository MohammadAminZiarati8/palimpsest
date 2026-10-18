#!/bin/sh
# prints the scene header, details and the visible room options (+ chances)
unity command eval --code 'var sb = new System.Text.StringBuilder();
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(UnityEngine.FindObjectsSortMode.None))
  if (t.gameObject.activeInHierarchy && (t.name == "Header" || t.name == "Details" || (t.transform.parent.name.StartsWith("Option") && t.transform.parent.name != "OptionInfo")))
    sb.Append(t.transform.parent.name + "/" + t.name + ": " + System.Text.RegularExpressions.Regex.Replace(t.text, "<[^>]+>", "").Replace("\n", " | ") + "\n");
return sb.ToString();' --no-banner --caller plugin --skill unity-cli --result-only 2>&1 | python -I -c "import sys,json; d=json.load(sys.stdin); print(d.get('result') or d)"
