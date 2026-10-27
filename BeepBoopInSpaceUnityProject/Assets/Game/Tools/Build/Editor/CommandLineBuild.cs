using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Tools.Build.Editor
{
    /// <summary>
    /// Headless Windows build:
    /// Unity.exe -batchmode -quit -projectPath &lt;project&gt; -executeMethod Game.Tools.Build.Editor.CommandLineBuild.BuildWindows
    ///     [-buildOutput &lt;folder&gt;] [-buildDevelopment]
    /// Without -buildOutput, builds into Builds/BeepBoop_Win_yyyyMMdd_HHmm[_Debug].
    /// </summary>
    public static class CommandLineBuild
    {
        private const string k_BuildAddressablesWithPlayerPreference = "Addressables.BuildAddressablesWithPlayerBuild";

        public static void BuildWindows()
        {
            var args = Environment.GetCommandLineArgs();
            var development = args.Contains("-buildDevelopment");
            var output = GetArgument(args, "-buildOutput")
                         ?? Path.Combine("Builds", $"BeepBoop_Win_{DateTime.Now:yyyyMMdd_HHmm}{(development ? "_Debug" : "")}");

            if (!BuildAddressablesIfNotBuiltWithPlayer())
            {
                EditorApplication.Exit(1);
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = Path.Combine(output, $"{PlayerSettings.productName}.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };

            var summary = BuildPipeline.BuildPlayer(options).summary;
            Debug.Log($"[CommandLineBuild] {summary.result}: {options.locationPathName}, {summary.totalSize / (1024 * 1024)} MB, " +
                      $"{summary.totalErrors} error(s), {summary.totalWarnings} warning(s), {summary.totalTime.TotalSeconds:0}s");

            if (summary.result != BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        // Addressables content is built with the player only when the settings (or the editor preference) say so.
        private static bool BuildAddressablesIfNotBuiltWithPlayer()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var builtWithPlayer = settings.BuildAddressablesWithPlayerBuild switch
            {
                AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer => true,
                AddressableAssetSettings.PlayerBuildOption.PreferencesValue => EditorPrefs.GetBool(k_BuildAddressablesWithPlayerPreference, true),
                _ => false,
            };
            if (builtWithPlayer)
                return true;

            Debug.Log("[CommandLineBuild] Building Addressables content before the player.");
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (string.IsNullOrEmpty(result.Error))
                return true;

            Debug.LogError($"[CommandLineBuild] Addressables build failed: {result.Error}");
            return false;
        }

        private static string GetArgument(string[] args, string name)
        {
            var index = Array.IndexOf(args, name);
            return index >= 0 && index < args.Length - 1 ? args[index + 1] : null;
        }
    }
}
