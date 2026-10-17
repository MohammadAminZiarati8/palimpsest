using System.IO;
using Game.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// M2a-T7: menu Game/Test Fight. Creates Assets/_Game/Scenes/TestFight.unity (camera + FightScreen)
    /// the first time, opens it and enters Play mode. Change heroes/enemies/seed on the "Fight" object.
    /// </summary>
    public static class TestFightMenu
    {
        private const string ScenePath = "Assets/_Game/Scenes/TestFight.unity";

        [MenuItem("Game/Test Fight")]
        public static void Open()
        {
            if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath);
            else
                CreateScene();
            EditorApplication.isPlaying = true;
        }

        private static void CreateScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            new GameObject("Fight").AddComponent<FightScreen>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[TestFight] Created {ScenePath}");
        }
    }
}
