using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using UnityEngine;

namespace Pinwheel.Contour
{
    public static class LinkStripDrawer
    {
        private static List<string> s_linkLabels = new List<string>();
        private static Dictionary<string, string> s_linkContents = new Dictionary<string, string>()
        {
            {"Docs",  "https://docs.pinwheelstud.io/memo"},
            {"Support",  "https://discord.gg/btX4pdmZdV"},
            {"|", "" },
            {"Poseidon", "https://assetstore.unity.com/packages/vfx/shaders/low-poly-water-poseidon-153826" },
            {"Jupiter" ,"https://assetstore.unity.com/packages/2d/textures-materials/sky/procedural-sky-shader-day-night-cycle-jupiter-159992" },
            {"Beam","https://assetstore.unity.com/packages/vfx/shaders/fullscreen-camera-effects/beam-froxel-based-volumetric-lighting-fog-urp-render-graph-317850" }
        };

        private static GUIStyle s_linkStyle;
        private static GUIStyle linkStyle
        {
            get
            {
                if (s_linkStyle == null)
                {
                    s_linkStyle = new GUIStyle(EditorStyles.miniLabel);
                }
                s_linkStyle.normal.textColor = new Color32(125, 170, 240, 255);
                s_linkStyle.alignment = TextAnchor.MiddleLeft;
                return s_linkStyle;
            }
        }

        public static void Draw(string utmCampaign = "", string utmSource = "", string utmMedium = "")
        {
            s_linkLabels.Clear();
            s_linkLabels.AddRange(s_linkContents.Keys);
            Rect r = EditorGUILayout.GetControlRect(false, 12);
            var rects = EditorGUIUtility.GetFlowLayoutedRects(r, linkStyle, 4, 0, s_linkLabels);

            for (int i = 0; i < rects.Count; ++i)
            {
                Rect rect = rects[i];
                string label = s_linkLabels[i];
                string url = s_linkContents[label];

                if (!string.IsNullOrEmpty(url))
                {
                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
                    if (GUI.Button(rect, label, linkStyle))
                    {
                        url = NetUtils.ModURL(url, utmCampaign, utmSource, $"{utmMedium}-{label.Replace(" ", "")}");
                        Application.OpenURL(url);
                    }
                }
                else
                {
                    GUI.Label(rect, label, linkStyle);
                }
            }
        }
    }
}