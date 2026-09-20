using UnityEditor;
using UnityEngine;

namespace Game.Global.InputIndicationsManagement.Editor
{
    [CustomEditor(typeof(InputSpriteDataAsset))]
    public class InputSpriteDataAssetEditor : UnityEditor.Editor
    {
        private const float PreviewSize = 100f;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            var castedTarget = (InputSpriteDataAsset)target;

            if (castedTarget.InputSprite != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

                Texture2D preview = AssetPreview.GetAssetPreview(castedTarget.InputSprite);

                // Fallback pendant que Unity génère la preview de manière asynchrone
                if (preview == null)
                {
                    preview = AssetPreview.GetMiniThumbnail(castedTarget.InputSprite);
                    Repaint(); // force un nouveau passage pour récupérer la vraie preview dès qu'elle est prête
                }

                if (preview != null)
                {
                    Rect rect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.ExpandWidth(false));
                    GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
                }
            }
        }
    }
}
