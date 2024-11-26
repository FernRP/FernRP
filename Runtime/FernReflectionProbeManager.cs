using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    struct FernReflectionProbeManager : IDisposable
    {
        static class ShaderProperties
        {
            public static readonly string AmbientSkyCube = "_AmbientSkyCube";
        }
        
        private static RTHandle blackCubemapRT;

        public static FernReflectionProbeManager Create()
        {
            var instance = new FernReflectionProbeManager();
            
            RenderTextureDescriptor desc = new RenderTextureDescriptor();
            desc.colorFormat = RenderTextureFormat.ARGBHalf;
            desc.dimension = TextureDimension.Cube;
            desc.width = 2;
            desc.height = 2;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(ref blackCubemapRT, desc, FilterMode.Bilinear, TextureWrapMode.Clamp,
                name: "_AmbientSkyCube");
            
            return instance;
        }

        public unsafe void UpdateGpuData(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var probes = renderingData.cullResults.visibleReflectionProbes;
            var probeCount = math.min(probes.Length, UniversalRenderPipeline.maxVisibleReflectionProbes);

            cmd.SetGlobalTexture(ShaderProperties.AmbientSkyCube, blackCubemapRT);

            for (var probeIndex = 0; probeIndex < probeCount; probeIndex++)
            {
                var reflectionProbe = probes[probeIndex].reflectionProbe;
                var fernProbe = reflectionProbe.GetAdditionalReflectionProbe();
                if (fernProbe.realtimeAmbient)
                {
                    reflectionProbe.mode = ReflectionProbeMode.Realtime;
                    reflectionProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                    reflectionProbe.RenderProbe();
                    cmd.SetGlobalTexture(ShaderProperties.AmbientSkyCube, reflectionProbe.texture?reflectionProbe.texture:blackCubemapRT);
                }
            }
        }

       
        public void Dispose()
        {
            
        }
    }
}
