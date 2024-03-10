using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    public class RSMBufferPass : ScriptableRenderPass
    {
        static readonly int s_CameraNormalsTextureID = Shader.PropertyToID("_CameraNormalsTexture");
        static ShaderTagId s_ShaderTagLit = new ShaderTagId("Lit");
        static ShaderTagId s_ShaderTagSimpleLit = new ShaderTagId("SimpleLit");
        static ShaderTagId s_ShaderTagUnlit = new ShaderTagId("Unlit");
        static ShaderTagId s_ShaderTagUniversalGBuffer = new ShaderTagId("UniversalGBuffer");
        static ShaderTagId s_ShaderTagUniversalMaterialType = new ShaderTagId("UniversalMaterialType");
        
        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Render RSMBuffer");
        
        private RSMVolume m_VolumeComponent;
        
        internal RTHandle[] RSMbufferAttachments { get; set; }
        internal RTHandle[] RSMInputAttachments { get; set; }
        private RTHandle[] RSMbufferRTHandles;
        internal TextureHandle[] RSMbufferTextureHandles { get; set; }
        
        internal GraphicsFormat[] RSMbufferFormats { get; set; }
        
        internal static readonly string[] k_GBufferNames = new string[]
        {
            "_RSMBuffer0",
            "_RSMBuffer1",
            "_RSMBuffer2"
        };
        
        internal bool UseRenderPass { get; set; }
        
        // Color buffer count (not including dephStencil).
        internal int RSMBufferSliceCount { get { return 2 + (UseRenderPass ? 1 : 0); } }

        
        // Not all platforms support R8G8B8A8_SNorm, so we need to check for the support and force accurate GBuffer normals and relevant shader variants
        private bool m_AccurateGbufferNormals;
        internal bool AccurateGbufferNormals
        {
            get { return m_AccurateGbufferNormals; }
            set { m_AccurateGbufferNormals = value || !RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R8G8B8A8_SNorm, FormatUsage.Render); }
        }
        
        internal int RSMBufferNormalIndex { get { return 0; } }
        internal int RSMBufferViewPositionIndex { get { return 1; } }
        
        internal bool UseLightLayers { get { return UniversalRenderPipeline.asset.useRenderingLayers; } }
        
        internal bool UseDecalLayers { get; set; }
        
        internal bool UseRenderingLayers { get { return UseLightLayers || UseDecalLayers; } }
        
        static ShaderTagId[] s_ShaderTagValues;
        static RenderStateBlock[] s_RenderStateBlocks;
        
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
            if (index == RSMBufferNormalIndex)
                return this.AccurateGbufferNormals ? GraphicsFormat.R8G8B8A8_UNorm : GraphicsFormat.R8G8B8A8_SNorm; // normal normal normal packedSmoothness
            else if (index == RSMBufferViewPositionIndex) // Optional: shadow mask is outputed in mixed lighting subtractive mode for non-static meshes only
                return GraphicsFormat.B8G8R8A8_UNorm;
            else
                return GraphicsFormat.None;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            CreateGbufferResources();
            
            RTHandle[] gbufferAttachments = RSMbufferAttachments;
            
            if (cmd != null)
            {
                // Create and declare the render targets used in the pass
                for (int i = 0; i < gbufferAttachments.Length; ++i)
                {
                    // No need to setup temporaryRTs if we are using input attachments as they will be Memoryless
                    if (UseRenderPass)
                        continue;
                    
                    ReAllocateGBufferIfNeeded(cameraTextureDescriptor, i);
                    
                    cmd.SetGlobalTexture(RSMbufferAttachments[i].name, RSMbufferAttachments[i].nameID);
                }
            }
            
            if (UseRenderPass)
                UpdateRSMInputAttachments();
            
            ConfigureTarget(RSMbufferAttachments, depthAttachmentHandle, RSMbufferFormats);
            
            // We must explicitly specify we don't want any clear to avoid unwanted side-effects.
            // ScriptableRenderer will implicitly force a clear the first time the camera color/depth targets are bound.
            ConfigureClear(ClearFlag.None, Color.black);
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
            var stack = VolumeManager.instance.stack;
            m_VolumeComponent = stack.GetComponent<RSMVolume>();
            if(!m_VolumeComponent.IsActive()) return;
            
            var cmd = renderingData.commandBuffer;
            m_PassData.filteringSettings = m_FilteringSettings;
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                ref CameraData cameraData = ref renderingData.cameraData;
                ShaderTagId lightModeTag = s_ShaderTagUniversalGBuffer;
                m_PassData.drawingSettings = CreateDrawingSettings(lightModeTag, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                
                ExecutePass(context, m_PassData, ref renderingData);
            }
        }

        static void ExecutePass(ScriptableRenderContext context, PassData data, ref RenderingData renderingData,
            bool useRenderGraph = false)
        {
            context.ExecuteCommandBuffer(renderingData.commandBuffer);
            renderingData.commandBuffer.Clear();

            NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
            NativeArray<RenderStateBlock> stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
            
            context.DrawRenderers(renderingData.cullResults, ref data.drawingSettings, ref data.filteringSettings, s_ShaderTagUniversalMaterialType, false, tagValues, stateBlocks);

            tagValues.Dispose();
            stateBlocks.Dispose();
            
            // Render objects that did not match any shader pass with error shader
            RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, renderingData.cameraData.camera, data.filteringSettings, SortingCriteria.None);
    }
        
        internal void ReAllocateGBufferIfNeeded(RenderTextureDescriptor gbufferSlice, int gbufferIndex)
        {
            if (this.RSMbufferRTHandles != null)
            {
                // In case DeferredLight does not own the RTHandle, we can skip realloc.
                if (this.RSMbufferRTHandles[gbufferIndex].GetInstanceID() != this.RSMbufferRTHandles[gbufferIndex].GetInstanceID())
                    return;

                gbufferSlice.depthBufferBits = 0; // make sure no depth surface is actually created
                gbufferSlice.stencilFormat = GraphicsFormat.None;
                gbufferSlice.graphicsFormat = this.GetGBufferFormat(gbufferIndex);
                RenderingUtils.ReAllocateIfNeeded(ref this.RSMbufferRTHandles[gbufferIndex], gbufferSlice, FilterMode.Point, TextureWrapMode.Clamp, name: DeferredLights.k_GBufferNames[gbufferIndex]);
                this.RSMbufferRTHandles[gbufferIndex] = this.RSMbufferRTHandles[gbufferIndex];
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
        
        internal void UpdateRSMInputAttachments()
        {
            this.RSMInputAttachments[0] = this.RSMbufferAttachments[0];
            this.RSMInputAttachments[1] = this.RSMbufferAttachments[1];
            this.RSMInputAttachments[2] = this.RSMbufferAttachments[2];
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