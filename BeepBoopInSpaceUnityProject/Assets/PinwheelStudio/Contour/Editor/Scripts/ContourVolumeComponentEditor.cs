using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using System.Collections.Generic;

namespace Pinwheel.Contour
{
    [CustomEditor(typeof(ContourEffectVolumeComponent))]
    public class ContourVolumeComponentEditor : VolumeComponentEditor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            LinkStripDrawer.Draw("", "contour", "volume");
        }
    }
}