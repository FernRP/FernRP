using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.FernRenderPipeline
{
    [System.Serializable, VolumeComponentMenu("FernRP/Depth Offset")]
    public class DepthOffset : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter isEnable = new BoolParameter(false);
        public ClampedIntParameter downSample = new ClampedIntParameter(0, 1, 4);

        public bool IsActive() => isEnable.value;
        public bool IsTileCompatible() => true;
    }

    [FernRender("Depth Offset", FernPostProcessInjectionPoint.BeforeOpaque)]
    public class DepthOffsetRender : FernRPFeatureRenderer
    {
        private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DepthShadowPrePass");

        private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("DepthShadowOnly");
        private ShaderTagId shaderTagId { get; set; } = k_ShaderTagId;

        private DepthOffset m_Component;
        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;
        internal DebugRendererLists debugRendererLists;

        public override ScriptableRenderPassInput input => ScriptableRenderPassInput.Depth;

        public override bool visibleInSceneView => true;
        
        RTHandle depthShadowRTHandle;
        
        static readonly int s_DrawDepthOffsetPassDataPropID = Shader.PropertyToID("_DrawDepthOffsetPassData");

        public override void Initialize()
        {
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        }

        public override bool Setup(ref RenderingData renderingData, FernPostProcessInjectionPoint injectionPoint, Material uberMaterial = null)
        {
            var stack = VolumeManager.instance.stack;
            m_Component = stack.GetComponent<DepthOffset>();
            if (!m_Component.IsActive())
            {
                return false;
            }            

            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.graphicsFormat = GraphicsFormat.None;
            descriptor.depthStencilFormat = descriptor.depthStencilFormat;
            descriptor.depthBufferBits = 16;
            descriptor.msaaSamples = 1;
            descriptor.width >>= m_Component.downSample.value;
            descriptor.height >>= m_Component.downSample.value;
            
            RenderingUtils.ReAllocateIfNeeded(ref depthShadowRTHandle, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CameraDepthShadowTexture");
            
            return true;
        }

        public override void Render(CommandBuffer cmd, ScriptableRenderContext context, FernCoreFeatureRenderPass.PostProcessRTHandles rtHandles, 
            ref RenderingData renderingData, FernPostProcessInjectionPoint injectionPoint)
        {
            
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                CoreUtils.SetRenderTarget(cmd, depthShadowRTHandle);
                CoreUtils.ClearRenderTarget(cmd, ClearFlag.All, Color.clear);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            
                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = RenderingUtils.CreateDrawingSettings(shaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None; 
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);
                cmd.SetGlobalTexture(depthShadowRTHandle.name, depthShadowRTHandle.nameID);
            }
        }
        
        internal class DepthOffsetData
        {
            internal RendererListHandle rendererList;
        }
        
        private RendererListParams InitRendererListParams(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData)
        {
            var sortFlags = cameraData.defaultOpaqueSortFlags;
            var drawSettings = RenderingUtils.CreateDrawingSettings(this.shaderTagId, renderingData, cameraData, lightData, sortFlags);
            drawSettings.perObjectData = PerObjectData.None;
            return new RendererListParams(renderingData.cullResults, drawSettings, m_FilteringSettings);
        }
        
        private static void ExecutePass(RasterCommandBuffer cmd, RendererList rendererList)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.DepthPrepass)))
            {
                cmd.DrawRendererList(rendererList);
            }
        }
        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

          
            
            RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
            descriptor.msaaSamples = 1;
            descriptor.width >>= m_Component.downSample.value;
            descriptor.height >>= m_Component.downSample.value;
            descriptor.graphicsFormat = GraphicsFormat.None;
            descriptor.depthStencilFormat = GraphicsFormat.D32_SFloat_S8_UInt;
          
            var depthShadowTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_CameraDepthShadowTexture", true);
            
            var colorTarget = depthShadowTexture;
            var depthTarget = resourceData.backBufferDepth;
            
            using (var builder = 
                   renderGraph.AddRasterRenderPass<DepthOffsetData>("Depth Offset Pass", out var passData, m_ProfilingSampler))
            {

                var param = InitRendererListParams(renderingData, cameraData, lightData);
                param.filteringSettings.batchLayerMask = uint.MaxValue;
                passData.rendererList = renderGraph.CreateRendererList(param);
                builder.UseRendererList(passData.rendererList);
                
                builder.SetRenderAttachmentDepth(colorTarget, AccessFlags.Write);
                
                builder.SetGlobalTextureAfterPass(depthShadowTexture, Shader.PropertyToID("_CameraDepthShadowTexture"));
                
                //  TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                
                if (cameraData.xr.enabled)
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && cameraData.xrUniversal.canFoveateIntermediatePasses);

                builder.SetRenderFunc((DepthOffsetData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data.rendererList);
                });
                
            }
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            depthShadowRTHandle?.Release();
        }
    }
}

