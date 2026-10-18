using System.IO;
using Game.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M2a-T7: menu Game/Test Fight (FightScreen test picker). M2b-T7: menu Game/Play Stages (StageScreen).
    /// Each creates its scene under Assets/_Game/Scenes the first time (camera + screen object), opens it
    /// and enters Play mode. Change heroes/enemies/seed of the test fight on the "Fight" object.
    /// </summary>
    public static class TestFightMenu
    {
        private const string FightScene = "Assets/_Game/Scenes/TestFight.unity";
        private const string StagesScene = "Assets/_Game/Scenes/Stages.unity";

        [MenuItem("Game/Test Fight")]
        public static void Open() => Play(FightScene, () => new GameObject("Fight").AddComponent<FightScreen>());

        [MenuItem("Game/Play Stages")]
        public static void OpenStages() => Play(StagesScene, () => new GameObject("Stages").AddComponent<StageScreen>());

        private static void Play(string path, System.Action fill)
        {
            if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (File.Exists(path))
                EditorSceneManager.OpenScene(path);
            else
                CreateScene(path, fill);
            EditorApplication.isPlaying = true;
        }

        private static void CreateScene(string path, System.Action fill)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            fill();
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
            Debug.Log($"[Game] Created {path}");
        }
    }
}
