using UnityEditor;
using UnityEngine;

namespace Game.Gameplay.Levels._0_Core.Editor
{
    public class CurrentLevelInfoEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Levels/Current Level Info Editor Window")]
        public static void ShowWindow()
        {
            // Obtenez une instance de la fenêtre ou créez-en une nouvelle
            CurrentLevelInfoEditorWindow window = GetWindow<CurrentLevelInfoEditorWindow>("Current Level Info Editor Window");
            window.minSize = new Vector2(300, 200);
        }
        
        private void OnGUI()
        {
            var levelDataAsset = AssetDatabase.LoadAssetAtPath<LevelDataAsset>(EditorPrefs.GetString("BB_LDA"));
            levelDataAsset = EditorGUILayout.ObjectField("Level Data Asset to use:", levelDataAsset, typeof(LevelDataAsset), false) as LevelDataAsset;
            EditorPrefs.SetString("BB_LDA", AssetDatabase.GetAssetPath(levelDataAsset));
        }
    }
}
