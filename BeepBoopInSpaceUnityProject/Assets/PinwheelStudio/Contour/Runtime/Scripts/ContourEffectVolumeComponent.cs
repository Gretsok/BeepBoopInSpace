using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pinwheel.Contour
{
    [VolumeComponentMenu("Post-processing Custom/Contour")]
    [VolumeRequiresRendererFeatures(typeof(ContourEffectRendererFeature))]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class ContourEffectVolumeComponent : VolumeComponent, IPostProcessComponent
    {
        public ContourEffectVolumeComponent()
        {
            displayName = "Contour";
        }

        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Header("Edge Detection")]
        public FloatParameter peakBrightness = new FloatParameter(2);
        public ClampedFloatParameter depthResponse = new ClampedFloatParameter(5f, 0f, 10f);

        [Header("Edge Enhancement")]
        public ClampedFloatParameter weakEdgeRemoval = new ClampedFloatParameter(0.5f, 0f, 1f);
        public ClampedFloatParameter strongEdgeHighlight = new ClampedFloatParameter(0.5f, 0f, 1f);
        public ClampedFloatParameter edgeClarity = new ClampedFloatParameter(0.5f, 0f, 1f);
        public MinFloatParameter fadeDistanceStart = new MinFloatParameter(50, 0);
        public MinFloatParameter fadeDistanceEnd = new MinFloatParameter(200, 0);
        public FloatParameter fadeBottomStart = new FloatParameter(-1000);
        public FloatParameter fadeBottomEnd = new FloatParameter(-1100);
        public FloatParameter fadeTopStart = new FloatParameter(1000);
        public FloatParameter fadeTopEnd = new FloatParameter(1100);

        [Header("Artistic")]
        public ColorParameter edgeColor = new ColorParameter(Color.black, true, false, true);
        public ColorParameter nonEdgeColor = new ColorParameter(Color.white, false, false, true);
        public ClampedIntParameter thickness = new ClampedIntParameter(1, 1, 4);

        public bool IsActive()
        {
            return this.active && intensity.GetValue<float>() > 0.0f;
        }

        public override void Override(VolumeComponent state, float interpFactor)
        {
            base.Override(state, interpFactor);

            fadeDistanceEnd.value = Mathf.Max(fadeDistanceEnd.value, fadeDistanceStart.value + 0.01f);
            fadeBottomStart.value = Mathf.Max(fadeBottomStart.value, fadeBottomEnd.value + 0.01f);
            fadeTopEnd.value = Mathf.Max(fadeTopEnd.value, fadeTopStart.value + 0.01f);
        }
    }
}