using Game.Core;
using Game.Shared.Champions;
using Game.Shared.Content;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Loads content the way the game does and logs every champion's Lv1 stats.
    /// Menu: Game/Check Content, or batch: -executeMethod Game.Editor.ContentCheck.Run
    /// </summary>
    public static class ContentCheck
    {
        [MenuItem("Game/Check Content")]
        public static void Run()
        {
            var db = ContentLoader.Load(new ResourcesContentSource());
            foreach (var hero in db.Champions.Values)
            {
                var s = StatCalculator.Level1(db, hero);
                Debug.Log($"[ContentCheck] {hero.Id} {hero.Name} ({hero.Class}, {hero.Rarity}): " +
                          $"HP {s.Hp} Atk {s.Attack} Def {s.Defense} Spd {s.Speed} Crit {s.CritChance}% CritDmg +{s.CritDamage}%");
            }
            Debug.Log($"[ContentCheck] OK: content v{db.Version}, {db.Champions.Count} champions");
        }
    }
}
