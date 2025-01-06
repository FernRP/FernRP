using UnityEngine.Rendering.FernRenderPipeline;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.FernRenderPipeline
{
    [System.Serializable, VolumeComponentMenu("FernRP/Edge Detection Outline")]
    public class EdgeDetectionOutlineEffect : VolumeComponent
    {
        [Tooltip("Controls the Effect Intensity")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 1);
        
        [Tooltip("Controls the edge thickness.")]
        public ClampedFloatParameter thickness = new ClampedFloatParameter(1, 0, 8);
        
        [FormerlySerializedAs("normalThreshold")] [Tooltip("Controls the threshold of the normal difference in degrees.")]
        public ClampedFloatParameter angleThreshold = new ClampedFloatParameter(1, 1, 179.9f);

        [Tooltip("Controls the threshold of the depth difference in world units.")]
        public ClampedFloatParameter depthThreshold = new ClampedFloatParameter(0.01f, 0.001f, 1);

        [Tooltip("Controls the edge color.")]
        public ColorParameter color = new ColorParameter(Color.black, true, false, true);
        
        [Tooltip("Controls Sampler 4 Or 8 times")]
        public BoolParameter LowQuality = new BoolParameter(false);
    }

    [FernRender("Edge Detection", FernPostProcessInjectionPoint.BeforePostProcess)]
    public class EdgeDetectionEffectRenderer : FernRPFeatureRenderer
    {
        private EdgeDetectionOutlineEffect m_VolumeComponent;
        private RTHandle edgeDetectionRTHandle;
        private Material m_Material;
        private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Edge Detection");
        private bool m_SupportsR8RenderTextureFormat =  SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8);

        static class ShaderIDs {
            internal readonly static int Input = Shader.PropertyToID("_BlitTexture");
            internal readonly static int Threshold = Shader.PropertyToID("_Edge_Threshold");
            internal readonly static int Color = Shader.PropertyToID("_Edge_Color");
            internal readonly static string LowQuality = "_LowQuality";
            internal static readonly int s_CameraNormalsTextureID = Shader.PropertyToID("_CameraNormalsTexture");
        }
        
        public override bool visibleInSceneView => true;
        
        public override ScriptableRenderPassInput input => ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;

        public override void Initialize()
        {
            m_Material = CoreUtils.CreateEngineMaterial("Hidden/FernNPR/PostProcess/EdgeDetectionOutline");
        }

        public override bool Setup(ref RenderingData renderingData, FernPostProcessInjectionPoint injectionPoint, Material uberMaterial = null)
        {
            var stack = VolumeManager.instance.stack;
            m_VolumeComponent = stack.GetComponent<EdgeDetectionOutlineEffect>();
            if (m_VolumeComponent.intensity.value <= 0)
            {
                Shader.DisableKeyword("_EDGEDETECION");
                return false;
            }
            
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 0;
            descriptor.colorFormat = m_SupportsR8RenderTextureFormat ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;
            
            RenderingUtils.ReAllocateIfNeeded(ref edgeDetectionRTHandle, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_EdgeDetectionTexture");

            return true;
        }
        
        private class EdgeDetectionOutlinePassData
        {
            internal Material material;
            internal TextureHandle cameraColor;
            internal RTHandle destination;
            internal TextureHandle cameraNormalsTexture;
        }
        
        private void InitEdgeDetectionOutlinePassData(ref EdgeDetectionOutlinePassData data)
        {
            data.material = m_Material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            
            CreateRenderTextureHandles(renderGraph, resourceData, cameraData, out TextureHandle destination);
            
            // Get the resources
            TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
            TextureHandle cameraNormalsTexture = resourceData.cameraNormalsTexture;
            
            CoreUtils.SetKeyword(m_Material, "_EDGEDETECION", true);
            CoreUtils.SetKeyword(m_Material, ShaderIDs.LowQuality, m_VolumeComponent.LowQuality.value);

            using (IUnsafeRenderGraphBuilder builder =
                   renderGraph.AddUnsafePass<EdgeDetectionOutlinePassData>("Edge Detection", out var passData, m_ProfilingSampler))
            {
                // Shader keyword changes are considered as global state modifications
                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);
                
                passData.material = m_Material;
                passData.destination = edgeDetectionRTHandle;
                passData.cameraColor = resourceData.cameraColor;
                builder.UseTexture(destination, AccessFlags.ReadWrite);
                
                if (cameraDepthTexture.IsValid())
                    builder.UseTexture(cameraDepthTexture, AccessFlags.Read);
                
                if (cameraNormalsTexture.IsValid())
                {
                    builder.UseTexture(cameraNormalsTexture, AccessFlags.Read);
                    passData.cameraNormalsTexture = cameraNormalsTexture;
                }
                
                builder.SetRenderFunc((EdgeDetectionOutlinePassData data, UnsafeGraphContext rgContext) =>
                {
                    CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(rgContext.cmd);
                    RenderBufferLoadAction finalLoadAction = RenderBufferLoadAction.Load;
                    
                    // Setup
                    if (data.cameraColor.IsValid())
                        PostProcessUtils.SetSourceSize(cmd, data.cameraColor);
                    
                    if (data.cameraNormalsTexture.IsValid())
                        data.material.SetTexture(ShaderIDs.s_CameraNormalsTextureID, data.cameraNormalsTexture);
                    
                    float angleThreshold = m_VolumeComponent.angleThreshold.value;
                    float depthThreshold = m_VolumeComponent.depthThreshold.value;
                    Vector4 threshold = new Vector4(Mathf.Cos(angleThreshold * Mathf.Deg2Rad), m_VolumeComponent.thickness.value, depthThreshold, m_VolumeComponent.intensity.value);
                    cmd.SetGlobalVector(ShaderIDs.Threshold, threshold);
                    cmd.SetGlobalColor(ShaderIDs.Color, m_VolumeComponent.color.value);
                    CoreUtils.SetKeyword(cmd, "_EDGEDETECION", true);
                    
                    Blitter.BlitCameraTexture(cmd, data.destination, data.destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, data.material,  0);
                    
                    cmd.SetGlobalTexture("_EdgeDetectionTexture", data.destination);
                });

            }
        }

        private void CreateRenderTextureHandles(RenderGraph renderGraph, UniversalResourceData resourceData,
            UniversalCameraData cameraData, out TextureHandle edgeDetectionTexture)
        {
            var descriptor = cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.depthBufferBits = 0;
            descriptor.colorFormat = m_SupportsR8RenderTextureFormat ? RenderTextureFormat.R8 : descriptor.colorFormat;
            
            // Handles
            edgeDetectionTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_EdgeDetectionTexture", false, FilterMode.Bilinear);
        }


        public override void Render(CommandBuffer cmd, ScriptableRenderContext context, FernCoreFeatureRenderPass.PostProcessRTHandles rtHandles, ref RenderingData renderingData, FernPostProcessInjectionPoint injectionPoint)
        {
            if(m_Material == null) return;
            CoreUtils.SetKeyword(cmd, "_EDGEDETECION", true);
            float angleThreshold = m_VolumeComponent.angleThreshold.value;
            float depthThreshold = m_VolumeComponent.depthThreshold.value;
            Vector4 threshold = new Vector4(Mathf.Cos(angleThreshold * Mathf.Deg2Rad), m_VolumeComponent.thickness.value, depthThreshold, m_VolumeComponent.intensity.value);
            cmd.SetGlobalVector(ShaderIDs.Threshold, threshold);
            cmd.SetGlobalColor(ShaderIDs.Color, m_VolumeComponent.color.value);
            CoreUtils.SetKeyword(m_Material, ShaderIDs.LowQuality, m_VolumeComponent.LowQuality.value);
            Blitter.BlitCameraTexture(cmd, rtHandles.m_Source, edgeDetectionRTHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Material, 0);
            cmd.SetGlobalTexture("_EdgeDetectionTexture", edgeDetectionRTHandle.nameID);
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            edgeDetectionRTHandle?.Release();
        }
    }
}