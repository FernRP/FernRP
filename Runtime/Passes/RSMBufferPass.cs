using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    public class RSMBufferPass : ScriptableRenderPass
    {
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
            
            RTHandle[] gbufferAttachments = RSMbufferAttachments;
            
            if (cmd != null)
            {
                
            }
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var stack = VolumeManager.instance.stack;
            m_VolumeComponent = stack.GetComponent<RSMVolume>();
            if(!m_VolumeComponent.IsActive()) return;
            
            Debug.Log("RSM Pass");
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