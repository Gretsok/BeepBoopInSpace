using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Pinwheel.Contour
{
    public sealed class ContourEffectRendererFeature : ScriptableRendererFeature
    {
        #region FEATURE_FIELDS

        [SerializeField]
        private Material m_Material;
        [SerializeField]
        private bool m_UseFastMode = false;

        [Header("Edge Detection Source")]
        [SerializeField]
        private bool m_UseSceneColor = false;
        [SerializeField]
        private bool m_UseSceneDepth = true;
        [SerializeField]
        private bool m_UseSceneNormals = true;
        [SerializeField]
        private Inclusion m_Include = Inclusion.OpaqueTransparent;

        [Header("Rendering")]
        [SerializeField]
        private bool m_RenderEdgeOnly = false;

        public enum Inclusion
        {
            Opaque,
            OpaqueTransparent,
            OpaqueTransparentPostprocessing
        }

        private EdgeDetectionPass m_EdgeDetectionPass;

        private static readonly string kUseSceneColorKeyword = "_USE_SCENE_COLOR";
        private static readonly string kUseSceneDepthKeyword = "_USE_SCENE_DEPTH";
        private static readonly string kUseSceneNormalsKeyword = "_USE_SCENE_NORMALS";
        private static readonly string kRenderEdgeOnlyKeyword = "_EDGE_ONLY";
        private static readonly string kFastModeKeyword = "_FAST_MODE";

        #endregion

        #region FEATURE_METHODS

        public override void Create()
        {
            name = "Contour";

            if (m_Material == null)
                m_Material = Resources.Load<Material>("Contour/FullscreenEdgeDetection");

            if (m_Material)
                m_EdgeDetectionPass = new EdgeDetectionPass(name, m_Material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // Skip rendering if m_Material or the pass instance are null for whatever reason
            if (m_Material == null || m_EdgeDetectionPass == null)
                return;

            // This check makes sure to not render the effect to reflection probes or preview cameras as post-processing is typically not desired there
            if (renderingData.cameraData.cameraType == CameraType.Preview || renderingData.cameraData.cameraType == CameraType.Reflection)
                return;

            ContourEffectVolumeComponent contourVolume = VolumeManager.instance.stack?.GetComponent<ContourEffectVolumeComponent>();
            if (contourVolume == null || !contourVolume.IsActive())
                return;

            m_EdgeDetectionPass.renderPassEvent =
                m_Include == Inclusion.Opaque ? RenderPassEvent.AfterRenderingOpaques :
                m_Include == Inclusion.OpaqueTransparent ? RenderPassEvent.AfterRenderingTransparents :
                m_Include == Inclusion.OpaqueTransparentPostprocessing ? RenderPassEvent.AfterRenderingPostProcessing :
                RenderPassEvent.AfterRenderingPostProcessing;

            ScriptableRenderPassInput passInput = ScriptableRenderPassInput.None;
            if (m_UseSceneColor)
            {
                m_Material.EnableKeyword(kUseSceneColorKeyword);
                //Opaque color texture not required as we will read from a fullsize copy of the frame buffer
            }
            else
            {
                m_Material.DisableKeyword(kUseSceneColorKeyword);
            }

            if (m_UseSceneDepth)
            {
                m_Material.EnableKeyword(kUseSceneDepthKeyword);
                passInput |= ScriptableRenderPassInput.Depth;
                passInput |= ScriptableRenderPassInput.Normal; //world normal is required to remove some artifact when looking at surfaces parallel to view direction
            }
            else
            {
                m_Material.DisableKeyword(kUseSceneDepthKeyword);
            }

            if (m_UseSceneNormals)
            {
                m_Material.EnableKeyword(kUseSceneNormalsKeyword);
                passInput |= ScriptableRenderPassInput.Normal;
            }
            else
            {
                m_Material.DisableKeyword(kUseSceneNormalsKeyword);
            }

            if (m_RenderEdgeOnly)
            {
                m_Material.EnableKeyword(kRenderEdgeOnlyKeyword);
            }
            else
            {
                m_Material.DisableKeyword(kRenderEdgeOnlyKeyword);
            }

            if (m_UseFastMode)
            {
                m_Material.EnableKeyword(kFastModeKeyword);
            }
            else
            {
                m_Material.DisableKeyword(kFastModeKeyword);
            }

            m_EdgeDetectionPass.ConfigureInput(passInput);
            renderer.EnqueuePass(m_EdgeDetectionPass);
        }

        protected override void Dispose(bool disposing)
        {

        }

        #endregion

        private class EdgeDetectionPass : ScriptableRenderPass
        {
            private Material m_Material;
            private static MaterialPropertyBlock s_SharedPropertyBlock = new MaterialPropertyBlock();
            private static readonly int kBlitTexturePropertyId = Shader.PropertyToID("_BlitTexture");
            private static readonly int kBlitScaleBiasPropertyId = Shader.PropertyToID("_BlitScaleBias");
            private static readonly int kIntensityPropertyId = Shader.PropertyToID("_Intensity");

            private static readonly int kPeakBrightnessPropertyId = Shader.PropertyToID("_PeakBrightness");
            private static readonly int kDepthResponsePropertyId = Shader.PropertyToID("_DepthResponse");

            private static readonly int kWeakEdgeRemovalPropertyId = Shader.PropertyToID("_WeakEdgeRemoval");
            private static readonly int kStrongEdgeHighlightPropertyId = Shader.PropertyToID("_StrongEdgeHighlight");
            private static readonly int kEdgeClarityPropertyId = Shader.PropertyToID("_EdgeClarity");

            private static readonly int kFadeDistanceStart = Shader.PropertyToID("_FadeDistanceStart");
            private static readonly int kFadeDistanceEnd = Shader.PropertyToID("_FadeDistanceEnd");

            private static readonly int kFadeBottomStart = Shader.PropertyToID("_FadeBottomStart");
            private static readonly int kFadeBottomEnd = Shader.PropertyToID("_FadeBottomEnd");

            private static readonly int kFadeTopStart = Shader.PropertyToID("_FadeTopStart");
            private static readonly int kFadeTopEnd = Shader.PropertyToID("_FadeTopEnd");

            private static readonly int kEdgeColorPropertyId = Shader.PropertyToID("_EdgeColor");
            private static readonly int kNonEdgeColorPropertyId = Shader.PropertyToID("_NonEdgeColor");
            private static readonly int kThicknessPropertyId = Shader.PropertyToID("_Thickness");

            private RTHandle m_CopiedColor;

            public EdgeDetectionPass(string passName, Material material)
            {
                profilingSampler = new ProfilingSampler(passName);
                m_Material = material;
                requiresIntermediateTexture = true;
            }

#if UNITY_6000_0_OR_NEWER
            #region PASS_RENDER_GRAPH_PATH
            private class PassData
            {
                public Material material;
                public TextureHandle inputTexture;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                using (var builder = renderGraph.AddRasterRenderPass<EdgeDetectionPass.PassData>(passName, out var passData, profilingSampler))
                {
                    passData.material = m_Material;

                    var cameraColorDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
                    cameraColorDesc.name = "_CameraColor_Contour";
                    cameraColorDesc.clearBuffer = false;

                    TextureHandle destination = renderGraph.CreateTexture(cameraColorDesc);
                    passData.inputTexture = resourcesData.cameraColor;

                    builder.UseTexture(passData.inputTexture, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc((EdgeDetectionPass.PassData data, RasterGraphContext context) => ExecuteMainPass(data, context));

                    //Swap cameraColor to the new temp resource (destination) for the next pass
                    resourcesData.cameraColor = destination;
                }
            }

            private static void ExecuteMainPass(EdgeDetectionPass.PassData data, RasterGraphContext context)
            {
                ExecuteMainPass(context.cmd, data.inputTexture.IsValid() ? data.inputTexture : null, data.material);
            }
            #endregion
#endif

            #region PASS_NON_RENDER_GRAPH_PATH
            [System.Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                ResetTarget();
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_CopiedColor, GetCopyPassTextureDescriptor(renderingData.cameraData.cameraTargetDescriptor), name: "_EdgeDetectionPassCopyColor");
            }

            private static RenderTextureDescriptor GetCopyPassTextureDescriptor(RenderTextureDescriptor desc)
            {
                desc.msaaSamples = 1;
                desc.depthBufferBits = (int)DepthBits.None;

                return desc;
            }

            [System.Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                ref var cameraData = ref renderingData.cameraData;
                var cmd = CommandBufferPool.Get();

                using (new ProfilingScope(cmd, profilingSampler))
                {
                    RasterCommandBuffer rasterCmd = CommandBufferHelpers.GetRasterCommandBuffer(cmd);

                    CoreUtils.SetRenderTarget(cmd, m_CopiedColor);
                    ExecuteCopyColorPass(rasterCmd, cameraData.renderer.cameraColorTargetHandle);

                    CoreUtils.SetRenderTarget(cmd, cameraData.renderer.cameraColorTargetHandle, cameraData.renderer.cameraDepthTargetHandle);

                    ExecuteMainPass(rasterCmd, m_CopiedColor, m_Material);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                CommandBufferPool.Release(cmd);
            }

            private static void ExecuteCopyColorPass(RasterCommandBuffer cmd, RTHandle sourceTexture)
            {
                Blitter.BlitTexture(cmd, sourceTexture, new Vector4(1, 1, 0, 0), 0.0f, false);
            }
            #endregion

            #region PASS_SHARED_RENDERING_CODE
            private static void ExecuteMainPass(RasterCommandBuffer cmd, RTHandle sourceTexture, Material material)
            {
                s_SharedPropertyBlock.Clear();
                if (sourceTexture != null)
                    s_SharedPropertyBlock.SetTexture(kBlitTexturePropertyId, sourceTexture);

                // This uniform needs to be set for user materials with shaders relying on core Blit.hlsl to work as expected
                s_SharedPropertyBlock.SetVector(kBlitScaleBiasPropertyId, new Vector4(1, 1, 0, 0));

                ContourEffectVolumeComponent contourVolume = VolumeManager.instance.stack?.GetComponent<ContourEffectVolumeComponent>();
                if (contourVolume != null)
                {
                    s_SharedPropertyBlock.SetFloat(kIntensityPropertyId, contourVolume.intensity.value);
                    s_SharedPropertyBlock.SetFloat(kPeakBrightnessPropertyId, contourVolume.peakBrightness.value);
                    s_SharedPropertyBlock.SetFloat(kDepthResponsePropertyId, contourVolume.depthResponse.value);

                    s_SharedPropertyBlock.SetFloat(kWeakEdgeRemovalPropertyId, contourVolume.weakEdgeRemoval.value);
                    s_SharedPropertyBlock.SetFloat(kStrongEdgeHighlightPropertyId, contourVolume.strongEdgeHighlight.value);
                    s_SharedPropertyBlock.SetFloat(kEdgeClarityPropertyId, contourVolume.edgeClarity.value);

                    s_SharedPropertyBlock.SetFloat(kFadeDistanceStart, contourVolume.fadeDistanceStart.value);
                    s_SharedPropertyBlock.SetFloat(kFadeDistanceEnd, contourVolume.fadeDistanceEnd.value);

                    s_SharedPropertyBlock.SetFloat(kFadeBottomStart, contourVolume.fadeBottomStart.value);
                    s_SharedPropertyBlock.SetFloat(kFadeBottomEnd, contourVolume.fadeBottomEnd.value);

                    s_SharedPropertyBlock.SetFloat(kFadeTopStart, contourVolume.fadeTopStart.value);
                    s_SharedPropertyBlock.SetFloat(kFadeTopEnd, contourVolume.fadeTopEnd.value);

                    s_SharedPropertyBlock.SetColor(kEdgeColorPropertyId, contourVolume.edgeColor.value);
                    s_SharedPropertyBlock.SetColor(kNonEdgeColorPropertyId, contourVolume.nonEdgeColor.value);
                    s_SharedPropertyBlock.SetInt(kThicknessPropertyId, contourVolume.thickness.value);
                }
                cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1, s_SharedPropertyBlock);
            }
            #endregion
        }
    }
}