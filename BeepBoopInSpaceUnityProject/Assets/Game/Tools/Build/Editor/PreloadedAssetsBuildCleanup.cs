using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.InputSystem;

namespace Game.Tools.Build.Editor
{
    // The Input System adds its project-wide actions to the preloaded assets before a player build and removes them
    // after it, in memory only, while Unity saves the project settings during the build. Without a later save,
    // ProjectSettings.asset keeps them, and from then on the Input System neither adds nor removes them.

    internal class PreloadedAssetsBuildPreprocessor : IPreprocessBuildWithReport
    {
        // Before the Input System (0), so that it adds the actions itself and removes them after the build.
        public int callbackOrder => int.MinValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            var actions = InputSystem.actions;
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (actions != null && preloadedAssets.Contains(actions))
                PlayerSettings.SetPreloadedAssets(preloadedAssets.Where(asset => asset != actions).ToArray());
        }
    }

    internal class PreloadedAssetsBuildPostprocessor : IPostprocessBuildWithReport
    {
        // After the Input System (0) removed the actions.
        public int callbackOrder => int.MaxValue;

        public void OnPostprocessBuild(BuildReport report) => AssetDatabase.SaveAssets();
    }
}
