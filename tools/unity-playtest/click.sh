#!/bin/sh
# usage: click.sh ButtonName  -> invokes the active uGUI Button with that GameObject name
code="var b = System.Linq.Enumerable.FirstOrDefault(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(UnityEngine.FindObjectsSortMode.None), x => x.name == \"$1\" && x.gameObject.activeInHierarchy); if (b == null) return \"not found: $1\"; if (!b.interactable) return \"not interactable: $1\"; b.onClick.Invoke(); return \"clicked $1\";"
unity command eval --code "$code" --no-banner --caller plugin --skill unity-cli --result-only 2>&1
