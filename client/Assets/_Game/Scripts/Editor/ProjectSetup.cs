using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Applies the project's fixed player settings (D-64, OVR-7): Android, landscape, IL2CPP ARM64.
    /// Menu: Game/Apply Project Setup, or batch: -executeMethod Game.Editor.ProjectSetup.Apply
    /// </summary>
    public static class ProjectSetup
    {
        // Placeholder id until the title is decided (Q-24); must be final before any Play Store upload.
        private const string AndroidId = "com.ziarati.palimpsest";

        [MenuItem("Game/Apply Project Setup")]
        public static void Apply()
        {
            PlayerSettings.companyName = "Ziarati";
            PlayerSettings.productName = "Palimpsest";

            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, AndroidId);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Android landscape IL2CPP ARM64 applied.");
        }
    }
}
