using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using System.Collections.Generic;

namespace Pinwheel.Contour
{
    [CustomEditor(typeof(ContourEffectRendererFeature))]
    public class ContourRendererFeatureEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            LinkStripDrawer.Draw("", "contour", "rendererfeature");
        }
    }
}