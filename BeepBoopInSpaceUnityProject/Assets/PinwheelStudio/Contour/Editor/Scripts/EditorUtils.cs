using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.Contour
{
    public class EditorUtils
    {
        private static readonly List<string> s_MenuLabels = new List<string>()
        {
            "Documentation",
            "Support",
            "Polaris",
            "Poseidon"
        };

        private static readonly List<string> s_MenuLinks = new List<string>()
        {
            "https://docs.google.com/document/d/1dM1M6wfiXaf3N1c0FGBcNd2LuZ4Pp3dSlZ8zOaQFkgQ/edit?usp=sharing",
            "https://discord.gg/Bm9bGgePNC",
            "https://assetstore.unity.com/packages/tools/terrain/polaris-3-low-poly-terrain-tool-286886?aid=1100l3QbW&pubref=contour-editor",
            "https://assetstore.unity.com/packages/vfx/shaders/low-poly-water-poseidon-153826?aid=1100l3QbW&pubref=contour-editor"
        };

        private static GUIStyle s_MenuStyle;
        private static GUIStyle GetMenuStyle()
        {
            if (s_MenuStyle==null)
            {
                s_MenuStyle = new GUIStyle(EditorStyles.miniLabel);
            }
            s_MenuStyle.normal.textColor = new Color32(125, 170, 240, 255);
            return s_MenuStyle;
        }

        public static void DrawExternalLinks()
        {
            Rect r = EditorGUILayout.GetControlRect();
            var rects = EditorGUIUtility.GetFlowLayoutedRects(r, GetMenuStyle(), 4, 0, s_MenuLabels);

            for (int i = 0; i < rects.Count; ++i)
            {
                EditorGUIUtility.AddCursorRect(rects[i], MouseCursor.Link);
                if (GUI.Button(rects[i], s_MenuLabels[i], GetMenuStyle()))
                {
                    Application.OpenURL(s_MenuLinks[i]);
                }
            }
        }
    }
}