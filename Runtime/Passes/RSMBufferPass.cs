﻿using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// RSM is not suitable for combining with Unity's Directional ShadowMap,
    /// because as Distance increases, RSM's Flux will contain a lot of information.
    /// TODO: Adding support for additional light would be useful
    /// </summary>
    public class RSMBufferPass : ScriptableRenderPass
    {
        static ShaderTagId s_ShaderTagLit = new ShaderTagId("Lit");
        static ShaderTagId s_ShaderTagSimpleLit = new ShaderTagId("SimpleLit");
        static ShaderTagId s_ShaderTagUnlit = new ShaderTagId("Unlit");
        static ShaderTagId s_ShaderTagUniversalGBuffer = new ShaderTagId("UniversalGBuffer");
        static ShaderTagId s_ShaderTagUniversalMaterialType = new ShaderTagId("UniversalMaterialType");

        private const string RSMBUFFERRENDER = "_RSMBUFFERRENDER";
        
        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Render RSMBuffer");
        
        const GraphicsFormat k_DepthStencilFormat = GraphicsFormat.D32_SFloat_S8_UInt;
        const int k_DepthBufferBits = 32;

        private RSMVolume m_VolumeComponent;

        private RTHandle DepthAttachment { get; set; }
        private RTHandle depthAttachmentRTHandle;

        internal RTHandle[] RSMbufferAttachments { get; set; }
        internal RTHandle[] RSMInputAttachments { get; set; }
        private RTHandle[] RSMbufferRTHandles;
        internal TextureHandle[] RSMbufferTextureHandles { get; set; }
        
        internal GraphicsFormat[] RSMbufferFormats { get; set; }
        
        internal static readonly string[] k_GBufferNames = new string[] 
        {
            "_RSMFluxBuffer",
            "_RSMNormalBuffer",
        };
        
        // TODO: More than one RT may be required
        internal int RSMBufferSliceCount { get { return 2; } }

        internal int RSMFluxBufferIndex { get { return 0; } }
        internal int RSMNormalBufferIndex { get { return 1; } }
        
        static ShaderTagId[] s_ShaderTagValues;
        static RenderStateBlock[] s_RenderStateBlocks;
        
        bool m_CreateEmptyShadowmap;
        
        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;
        private PassData m_PassData;

        public RSMBufferPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
        {
            base.profilingSampler = new ProfilingSampler(nameof(RSMBufferPass));
            base.renderPassEvent = evt;
            m_PassData = new PassData();
            
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            
            m_RenderStateBlock.stencilState = stencilState;
            m_RenderStateBlock.stencilReference = stencilReference;
            m_RenderStateBlock.mask = RenderStateMask.Stencil;
            
            if (s_ShaderTagValues == null)
            {
                s_ShaderTagValues = new ShaderTagId[4];
                s_ShaderTagValues[0] = s_ShaderTagLit;
                s_ShaderTagValues[1] = s_ShaderTagSimpleLit;
                s_ShaderTagValues[2] = s_ShaderTagUnlit;
                s_ShaderTagValues[3] = new ShaderTagId(); // Special catch all case for materials where UniversalMaterialType is not defined or the tag value doesn't match anything we know.
            }
            
            if (s_RenderStateBlocks == null)
            {
                s_RenderStateBlocks = new RenderStateBlock[4];
                s_RenderStateBlocks[0] = DeferredLights.OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialLit);
                s_RenderStateBlocks[1] = DeferredLights.OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialSimpleLit);
                s_RenderStateBlocks[2] = DeferredLights.OverwriteStencil(m_RenderStateBlock, (int)StencilUsage.MaterialMask, (int)StencilUsage.MaterialUnlit);
                s_RenderStateBlocks[3] = s_RenderStateBlocks[0];
            }
        }

        internal GraphicsFormat GetGBufferFormat(int index)
        {
            if (index == RSMFluxBufferIndex) // Optional: shadow mask is outputed in mixed lighting subtractive mode for non-static meshes only
                return GraphicsFormat.B8G8R8A8_SRGB;
            else if (index == RSMNormalBufferIndex) // Optional: shadow mask is outputed in mixed lighting subtractive mode for non-static meshes only
                return GraphicsFormat.R8G8B8A8_UNorm;
            else
                return GraphicsFormat.None;
        }

        internal bool Setup(ref RenderingData renderingData, RSMVolume rsmVolume)
        {
            if (!renderingData.shadowData.mainLightShadowsEnabled)
                return false;

            if (!renderingData.shadowData.supportsMainLightShadows)
                return false;
            
            m_VolumeComponent = rsmVolume;
            if(!m_VolumeComponent.IsActive()) return false;
            
            CreateGbufferResources();
            

            // Depth
            var depthDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            if (!RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R32_SFloat, FormatUsage.Render))
            {
                depthDescriptor.graphicsFormat = GraphicsFormat.None;
                depthDescriptor.depthStencilFormat = k_DepthStencilFormat;
                depthDescriptor.depthBufferBits = k_DepthBufferBits;
            }
            else
            {
                depthDescriptor.graphicsFormat = GraphicsFormat.R32_SFloat;
                depthDescriptor.depthStencilFormat = GraphicsFormat.None;
                depthDescriptor.depthBufferBits = k_DepthBufferBits;
            }

            depthDescriptor.width = renderingData.shadowData.additionalLightsShadowmapWidth;
            depthDescriptor.height = renderingData.shadowData.additionalLightsShadowmapWidth;

            depthDescriptor.msaaSamples = 1;// Depth-Only pass don't use MSAA
            RenderingUtils.ReAllocateIfNeeded(ref depthAttachmentRTHandle, depthDescriptor, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name: "_RSMDepthTexture");
            this.DepthAttachment = depthAttachmentRTHandle;
            
            RTHandle[] gbufferAttachments = RSMbufferAttachments;
            
            // Create and declare the render targets used in the pass
            for (int i = 0; i < gbufferAttachments.Length; ++i)
            {
                ReAllocateGBufferIfNeeded(renderingData.cameraData.cameraTargetDescriptor, renderingData.shadowData.additionalLightsShadowmapWidth, i);
            }
            
            return true;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if(!m_VolumeComponent.IsActive()) return;
            
            if (cmd != null)
            {
                // Create and declare the render targets used in the pass
                for (int i = 0; i < RSMbufferAttachments.Length; ++i)
                {
                    cmd.SetGlobalTexture(RSMbufferAttachments[i].name, RSMbufferAttachments[i].nameID);
                }
            }
            
            cmd.SetGlobalTexture(depthAttachmentRTHandle.name, depthAttachmentRTHandle.nameID);
            
            ConfigureTarget(RSMbufferRTHandles, DepthAttachment, RSMbufferFormats);
            
            // We must explicitly specify we don't want any clear to avoid unwanted side-effects.
            // ScriptableRenderer will implicitly force a clear the first time the camera color/depth targets are bound.
            ConfigureClear(ClearFlag.All, Color.black);
        }
        
        private class PassData
        {
            internal TextureHandle[] gbuffer;
            internal TextureHandle depth;

            internal RenderingData renderingData;

            internal DeferredLights deferredLights;
            internal FilteringSettings filteringSettings;
            internal DrawingSettings drawingSettings;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;
            var stack = VolumeManager.instance.stack;
            m_VolumeComponent = stack.GetComponent<RSMVolume>();
            if (!m_VolumeComponent.IsActive())
            {
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.RSM, false);
                return;
            }
            
            m_PassData.filteringSettings = m_FilteringSettings;
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                cmd.EnableKeyword(GlobalKeyword.Create(RSMBUFFERRENDER));
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.RSM, true);
                ref CameraData cameraData = ref renderingData.cameraData;
                ShaderTagId lightModeTag = s_ShaderTagUniversalGBuffer;
                m_PassData.drawingSettings = CreateDrawingSettings(lightModeTag, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                
                ExecutePass(context, cmd, m_PassData, ref renderingData);
                
                cmd.DisableKeyword(GlobalKeyword.Create(RSMBUFFERRENDER));
            }
        }
        
        private void ExecutePass(ScriptableRenderContext context, CommandBuffer cmd, PassData data, ref RenderingData renderingData,
            bool useRenderGraph = false)
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            var cullResults = renderingData.cullResults;
            var lightData = renderingData.lightData;
            
            int rsmLightIndex = lightData.mainLightIndex;
            if (rsmLightIndex == -1)
                return;
            
            VisibleLight rsmLight = lightData.visibleLights[rsmLightIndex];
            
            Bounds bounds;
            if (!renderingData.cullResults.GetShadowCasterBounds(rsmLightIndex, out bounds))
                return;
            
            // TODO: splitIndex 0 for now, I Want to generate rsm buffer with shadowcaster
            Matrix4x4 viewMatrix;
            Matrix4x4 projectionMatrix;
            ShadowSplitData splitData;
            bool success = cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(rsmLightIndex,
                0, 1, Vector3.right, renderingData.shadowData.mainLightShadowmapWidth, rsmLight.light.shadowNearPlane, out viewMatrix, out projectionMatrix,
                out splitData);
            
            cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
            NativeArray<RenderStateBlock> stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
            
            context.DrawRenderers(renderingData.cullResults, ref data.drawingSettings, ref data.filteringSettings, s_ShaderTagUniversalMaterialType, false, tagValues, stateBlocks);

            cmd.SetViewProjectionMatrices(renderingData.cameraData.GetViewMatrix(), renderingData.cameraData.GetProjectionMatrix());
            cmd.SetGlobalFloat(ShaderPropertyId.rsmSampleCount, m_VolumeComponent.RSMSampleCount.value);
            cmd.SetGlobalFloat(ShaderPropertyId.rsmIntensity, m_VolumeComponent.RSMIntensity.value);
            Matrix4x4 VPMatrix = projectionMatrix * viewMatrix;
            VPMatrix.SetRow(1, -VPMatrix.GetRow(1));
            cmd.SetGlobalMatrix(ShaderPropertyId.inverseLightViewProjectionMatrix, VPMatrix.inverse);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            tagValues.Dispose();
            stateBlocks.Dispose();
            
            // Render objects that did not match any shader pass with error shader
            RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, renderingData.cameraData.camera, data.filteringSettings, SortingCriteria.None);
        }
        
        internal void ReAllocateGBufferIfNeeded(RenderTextureDescriptor gbufferSlice, int resolution, int gbufferIndex)
        {
            if (this.RSMbufferRTHandles != null)
            {
                // In case DeferredLight does not own the RTHandle, we can skip realloc.
                if (this.RSMbufferRTHandles[gbufferIndex].GetInstanceID() != this.RSMbufferAttachments[gbufferIndex].GetInstanceID())
                    return;
                gbufferSlice.depthBufferBits = 0; // make sure no depth surface is actually created
                gbufferSlice.stencilFormat = GraphicsFormat.None;
                gbufferSlice.graphicsFormat = this.GetGBufferFormat(gbufferIndex);
                gbufferSlice.width = resolution;
                gbufferSlice.height = resolution;
                RenderingUtils.ReAllocateIfNeeded(ref this.RSMbufferRTHandles[gbufferIndex], gbufferSlice, FilterMode.Point, TextureWrapMode.Clamp, name: k_GBufferNames[gbufferIndex]);
                this.RSMbufferAttachments[gbufferIndex] = this.RSMbufferRTHandles[gbufferIndex];
            }
        }
        
        internal void CreateGbufferResources()
        {
            int gbufferSliceCount = this.RSMBufferSliceCount;
            if (this.RSMbufferRTHandles == null || this.RSMbufferRTHandles.Length != gbufferSliceCount)
            {
                ReleaseGbufferResources();

                this.RSMbufferAttachments = new RTHandle[gbufferSliceCount];
                this.RSMbufferRTHandles = new RTHandle[gbufferSliceCount];
                this.RSMbufferFormats = new GraphicsFormat[gbufferSliceCount];
                this.RSMbufferTextureHandles = new TextureHandle[gbufferSliceCount];
                for (int i = 0; i < gbufferSliceCount; ++i)
                {
                    this.RSMbufferRTHandles[i] = RTHandles.Alloc(k_GBufferNames[i], name: k_GBufferNames[i]);
                    this.RSMbufferAttachments[i] = this.RSMbufferRTHandles[i];
                    this.RSMbufferFormats[i] = this.GetGBufferFormat(i);
                }
            }
        }
        
        internal void ReleaseGbufferResources()
        {
            if (this.RSMbufferRTHandles != null)
            {
                // Release the old handles before creating the new one
                for (int i = 0; i < this.RSMbufferRTHandles.Length; ++i)
                {
                    RTHandles.Release(this.RSMbufferRTHandles[i]);
                }
            }
        }
        
        
        public void Dispose()
        {
            // TODO:
        }
    }
}