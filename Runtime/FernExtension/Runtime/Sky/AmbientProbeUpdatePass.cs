using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
namespace UnityEngine.Rendering.Universal
{
    internal class AmbientProbeUpdatePass : ScriptableRenderPass
    {
        struct CameraTemporarySettings
        {
            public float fieldOfView;
            public float aspect;
        };
        
        static readonly int s_AmbientProbeOutputBufferParam = Shader.PropertyToID("_AmbientProbeOutputBuffer");
        static readonly int s_VolumetricAmbientProbeOutputBufferParam = Shader.PropertyToID("_VolumetricAmbientProbeOutputBuffer");
        static readonly int s_DiffuseAmbientProbeOutputBufferParam = Shader.PropertyToID("_DiffuseAmbientProbeOutputBuffer");
        static readonly int s_ScratchBufferParam = Shader.PropertyToID("_ScratchBuffer");
        static readonly int s_AmbientProbeInputCubemap = Shader.PropertyToID("_AmbientProbeInputCubemap");
        static readonly int s_FogParameters = Shader.PropertyToID("_FogParameters");

        private AmbientProbeUpdateVolume m_VolumeComponent;

        public Cubemap skyCubemap;
        
        public ComputeShader computeAmbientProbeCS;
        public ComputeBuffer ambientProbeResult { get; private set; }
        public ComputeBuffer diffuseAmbientProbeBuffer { get; private set; }
        public ComputeBuffer volumetricAmbientProbeBuffer { get; private set; }
        public ComputeBuffer scratchBuffer { get; private set; }
        
        SphericalHarmonicsL2 m_AmbientProbe;
        
        internal bool ambientProbeIsReady = false;
        
        public int computeAmbientProbeKernel;
        
        private static readonly int AmbientSkyCube = Shader.PropertyToID("_AmbientSkyCube");

        public AmbientProbeUpdatePass(RenderPassEvent renderPassEvent, PostProcessData data)
        {
            this.renderPassEvent = renderPassEvent;
            computeAmbientProbeCS = data.shaders.shConvolutionCS;
            computeAmbientProbeKernel = computeAmbientProbeCS.FindKernel("CSMain");

            
            // Compute buffer storing the resulting SH from diffuse convolution. L2 SH => 9 float per component.
            ambientProbeResult = new ComputeBuffer(27, 4);
            // Buffer is stored packed to be used directly by shader code (27 coeffs in 7 float4)
            // Compute buffer storing the pre-convolved resulting SH For volumetric lighting. L2 SH => 9 float per component.
            volumetricAmbientProbeBuffer = new ComputeBuffer(7, 16);
            // Compute buffer storing the diffuse convolution SH For diffuse ambient lighting. L2 SH => 9 float per component.
            diffuseAmbientProbeBuffer = new ComputeBuffer(7, 16);

            scratchBuffer = new ComputeBuffer(27, sizeof(uint));

        }

        public void Setup()
        {
            ClearAmbientProbe();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var stack = VolumeManager.instance.stack;
            m_VolumeComponent = stack.GetComponent<AmbientProbeUpdateVolume>();
            if(!m_VolumeComponent.IsActive()) return;
            
            var cmd = renderingData.commandBuffer;

            cmd.SetComputeBufferParam(computeAmbientProbeCS, computeAmbientProbeKernel, s_AmbientProbeOutputBufferParam, ambientProbeResult);
            cmd.SetComputeBufferParam(computeAmbientProbeCS, computeAmbientProbeKernel, s_ScratchBufferParam, scratchBuffer);
            cmd.SetComputeTextureParam(computeAmbientProbeCS, computeAmbientProbeKernel, s_AmbientProbeInputCubemap, skyCubemap);
            if (diffuseAmbientProbeBuffer != null)
                cmd.SetComputeBufferParam(computeAmbientProbeCS, computeAmbientProbeKernel, s_DiffuseAmbientProbeOutputBufferParam, diffuseAmbientProbeBuffer);
            
            Hammersley.BindConstants(cmd, computeAmbientProbeCS);
            
            cmd.DispatchCompute(computeAmbientProbeCS, computeAmbientProbeKernel, 1, 1, 1);
            if (ambientProbeResult != null)
            {
                cmd.RequestAsyncReadback(ambientProbeResult, OnComputeAmbientProbeDone);
            }
        }
        
        internal void SetupAmbientProbe()
        {
            // Order is important!
            RenderSettings.ambientMode = AmbientMode.Skybox; // Needed to specify ourselves the ambient probe (this will update internal ambient probe data passed to shaders)
            RenderSettings.ambientProbe = m_AmbientProbe;
        }
        
        public void ClearAmbientProbe()
        {
            m_AmbientProbe = new SphericalHarmonicsL2();
        }

        public void UpdateAmbientProbe(in SphericalHarmonicsL2 probe)
        {
            m_AmbientProbe = probe;
        }

        public void OnComputeAmbientProbeDone(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                var result = request.GetData<float>();
                for (int channel = 0; channel < 3; ++channel)
                {
                    for (int coeff = 0; coeff < 9; ++coeff)
                    {
                        m_AmbientProbe[channel, coeff] = result[channel * 9 + coeff];
                    }
                }

                ambientProbeIsReady = true;

                SetupAmbientProbe();
            }
        }
    }
}
