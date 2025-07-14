using System.Linq;
using Game.Characters;
using Game.Gameplay.Levels._0_Core.EditorUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Levels._0_Core.Editor
{
    public class StandaloneGameInfoEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Levels/Standalone Game Info Editor Window")]
        public static void ShowWindow()
        {
            StandaloneGameInfoEditorWindow window = GetWindow<StandaloneGameInfoEditorWindow>("Standalone Game Info Editor Window");
            window.minSize = new Vector2(300, 200);
        }
        
        private void OnGUI()
        {
            EditorPrefs.SetBool(LevelEditorPrefsConstants.OverridesGameInfosKey,
                EditorGUILayout.Toggle("Override game info in standalone", EditorPrefs.GetBool(LevelEditorPrefsConstants.OverridesGameInfosKey, true)));

            if (!EditorPrefs.GetBool(LevelEditorPrefsConstants.OverridesGameInfosKey))
                return;
            
            var levelDataAsset = AssetDatabase.LoadAssetAtPath<LevelDataAsset>(EditorPrefs.GetString(LevelEditorPrefsConstants.LevelDataAssetKey));
            levelDataAsset = EditorGUILayout.ObjectField("Level Data Asset to use:", levelDataAsset, typeof(LevelDataAsset), false) as LevelDataAsset;
            EditorPrefs.SetString(LevelEditorPrefsConstants.LevelDataAssetKey, AssetDatabase.GetAssetPath(levelDataAsset));

            EditorGUILayout.Space();
            
            EditorPrefs.SetInt(LevelEditorPrefsConstants.NumberOfPlayersKey, 
                EditorGUILayout.IntSlider("Number of players:", EditorPrefs.GetInt(LevelEditorPrefsConstants.NumberOfPlayersKey, 1),
                1, 4));

            var devices = InputSystem.devices.ToList().ConvertAll(device => device.name);
            
            for (int i = 0; i < EditorPrefs.GetInt(LevelEditorPrefsConstants.NumberOfPlayersKey); ++i)
            {
                EditorGUILayout.PrefixLabel($"Player #{i + 1}:");
                EditorPrefs.SetInt($"{LevelEditorPrefsConstants.DeviceAssignedKey}{i}", 
                    EditorGUILayout.Popup( EditorPrefs.GetInt($"{LevelEditorPrefsConstants.DeviceAssignedKey}{i}", 0), devices.ToArray()));
                
                var characterDataAsset = AssetDatabase.LoadAssetAtPath<CharacterDataAsset>(EditorPrefs.GetString($"{LevelEditorPrefsConstants.CharacterDataAssignedKey}{i}"));
                characterDataAsset = EditorGUILayout.ObjectField("Character Data Asset:", characterDataAsset, typeof(CharacterDataAsset), false) as CharacterDataAsset;
                EditorPrefs.SetString($"{LevelEditorPrefsConstants.CharacterDataAssignedKey}{i}", AssetDatabase.GetAssetPath(characterDataAsset));
            }
        }
    }
}
