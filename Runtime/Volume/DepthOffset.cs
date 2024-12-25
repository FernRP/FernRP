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
        private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("DepthShadowPrepass");

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
        

        public override bool Setup(ref RenderingData renderingData, FernPostProcessInjectionPoint injectionPoint, Material uberMaterial = null)
        {
            var stack = VolumeManager.instance.stack;
            m_Component = stack.GetComponent<DepthOffset>();
            if (!m_Component.IsActive())
            {
                return false;
            }            
            
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque);

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
            internal TextureHandle albedoHdl;
            internal TextureHandle depthHdl;
            
            internal UniversalCameraData cameraData;
            internal DebugRendererLists debugRendererLists;
            internal RendererListHandle rendererListHdl;
            internal RendererListHandle objectsWithErrorRendererListHdl;
            
            // Required for code sharing purpose between RG and non-RG.
            internal RendererList rendererList;
            internal RendererList objectsWithErrorRendererList;
        }

        internal void InitRendererLists(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData, ref DepthOffsetData passData, ScriptableRenderContext context, RenderGraph renderGraph, bool useRenderGraph)
        {
            ref Camera camera = ref cameraData.camera;
            var sortFlags = cameraData.defaultOpaqueSortFlags;
            if (cameraData.renderer.useDepthPriming && (cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth))
                sortFlags = SortingCriteria.SortingLayer | SortingCriteria.RenderQueue | SortingCriteria.OptimizeStateChanges | SortingCriteria.CanvasOrder;

            var filterSettings = m_FilteringSettings;
            filterSettings.batchLayerMask = uint.MaxValue;
#if UNITY_EDITOR
                // When rendering the preview camera, we want the layer mask to be forced to Everything
                if (cameraData.isPreviewCamera)
                {
                    filterSettings.layerMask = -1;
                }
#endif
            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shaderTagId, renderingData, cameraData, lightData, sortFlags);
            if (cameraData.renderer.useDepthPriming && (cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth))
            {
                m_RenderStateBlock.depthState = new DepthState(false, CompareFunction.Equal);
                m_RenderStateBlock.mask |= RenderStateMask.Depth;
            }
            else if (m_RenderStateBlock.depthState.compareFunction == CompareFunction.Equal)
            {
                m_RenderStateBlock.depthState = new DepthState(true, CompareFunction.LessEqual);
                m_RenderStateBlock.mask |= RenderStateMask.Depth;
            }

            var activeDebugHandler = ScriptableRenderPass.GetActiveDebugHandler(cameraData);
            if (useRenderGraph)
            {
                if (activeDebugHandler != null)
                {
                    passData.debugRendererLists = activeDebugHandler.CreateRendererListsWithDebugRenderState(renderGraph, ref renderingData.cullResults, ref drawSettings, ref filterSettings, ref m_RenderStateBlock);
                }
                else
                {
                    RenderingUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawSettings, filterSettings, m_RenderStateBlock, ref passData.rendererListHdl);
                    RenderingUtils.CreateRendererListObjectsWithError(renderGraph, ref renderingData.cullResults, camera, filterSettings, sortFlags, ref passData.objectsWithErrorRendererListHdl);
                }
            }
            else
            {
                if (activeDebugHandler != null)
                {
                    passData.debugRendererLists = activeDebugHandler.CreateRendererListsWithDebugRenderState(context, ref renderingData.cullResults, ref drawSettings, ref filterSettings, ref m_RenderStateBlock);
                }
                else
                {
                    RenderingUtils.CreateRendererListWithRenderStateBlock(context, ref renderingData.cullResults, drawSettings, filterSettings, m_RenderStateBlock, ref passData.rendererList);
                    RenderingUtils.CreateRendererListObjectsWithError(context, ref renderingData.cullResults, camera, filterSettings, sortFlags, ref passData.objectsWithErrorRendererList);
                }
            }
        }
        
        /// <summary>
        /// Initialize the shared pass data.
        /// </summary>
        /// <param name="passData"></param>
        internal void InitPassData(UniversalCameraData cameraData, ref DepthOffsetData passData, bool isActiveTargetBackBuffer = false)
        {
            passData.cameraData = cameraData;
        }

        
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            var colorTarget = TextureHandle.nullHandle;
            var depthTarget = resourceData.backBufferDepth;
            
            using (var builder = 
                   renderGraph.AddRasterRenderPass<DepthOffsetData>("Depth Offset", out var passData, m_ProfilingSampler))
            {
                builder.UseAllGlobalTextures(true);

                InitPassData(cameraData, ref passData, resourceData.isActiveTargetBackBuffer);
                
                if (colorTarget.IsValid())
                {
                    passData.albedoHdl = colorTarget;
                    builder.SetRenderAttachment(colorTarget, 0, AccessFlags.Write);
                }
                
                if (depthTarget.IsValid())
                {
                    passData.depthHdl = depthTarget;
                    builder.SetRenderAttachmentDepth(depthTarget, AccessFlags.Write);
                }
                
                InitRendererLists(renderingData, cameraData, lightData, ref passData, default(ScriptableRenderContext), renderGraph, true);
                var activeDebugHandler = ScriptableRenderPass.GetActiveDebugHandler(cameraData);
                if (activeDebugHandler != null)
                {
                    passData.debugRendererLists.PrepareRendererListForRasterPass(builder);
                }
                else
                {
                    builder.UseRendererList(passData.rendererListHdl);
                    builder.UseRendererList(passData.objectsWithErrorRendererListHdl);
                }
                
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                
                if (cameraData.xr.enabled)
                {
                    bool passSupportsFoveation = cameraData.xrUniversal.canFoveateIntermediatePasses || resourceData.isActiveTargetBackBuffer;
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);
                }
                
                builder.SetRenderFunc((DepthOffsetData data, RasterGraphContext context) =>
                {
                    bool yFlip = data.cameraData.IsRenderTargetProjectionMatrixFlipped(data.albedoHdl, data.depthHdl);
                    
                    ExecutePass(context.cmd, data, data.rendererListHdl, data.objectsWithErrorRendererListHdl, yFlip);
                });
                
            }
        }
        
        internal static void ExecutePass(RasterCommandBuffer cmd, DepthOffsetData data, RendererList rendererList, RendererList objectsWithErrorRendererList, bool yFlip)
        {
            // Global render pass data containing various settings.
            // x,y,z are currently unused
            // w is used for knowing whether the object is opaque(1) or alpha blended(0)
            Vector4 drawObjectPassData = new Vector4(0.0f, 0.0f, 0.0f, 1);
            cmd.SetGlobalVector(s_DrawDepthOffsetPassDataPropID, drawObjectPassData);

            // scaleBias.x = flipSign
            // scaleBias.y = scale
            // scaleBias.z = bias
            // scaleBias.w = unused
            float flipSign = yFlip ? -1.0f : 1.0f;
            Vector4 scaleBias = (flipSign < 0.0f)
                ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
            cmd.SetGlobalVector(ShaderPropertyId.scaleBiasRt, scaleBias);

            // Set a value that can be used by shaders to identify when AlphaToMask functionality may be active
            // The material shader alpha clipping logic requires this value in order to function correctly in all cases.
            float alphaToMaskAvailable = 1.0f;
            cmd.SetGlobalFloat(ShaderPropertyId.alphaToMaskAvailable, alphaToMaskAvailable);

            var activeDebugHandler = ScriptableRenderPass.GetActiveDebugHandler(data.cameraData);
            if (activeDebugHandler != null)
            {
                data.debugRendererLists.DrawWithRendererList(cmd);
            }
            else
            {
                cmd.DrawRendererList(rendererList);
                // Render objects that did not match any shader pass with error shader
                RenderingUtils.DrawRendererListObjectsWithError(cmd, ref objectsWithErrorRendererList);
            }
        }

        public override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            depthShadowRTHandle?.Release();
        }
    }
}

