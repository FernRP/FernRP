using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;


namespace UnityEngine.Rendering.Universal
{
    class SkyUpdatePass : ScriptableRenderPass
    {
        
        static readonly int s_AmbientProbeOutputBufferParam = Shader.PropertyToID("_AmbientProbeOutputBuffer");
        static readonly int s_VolumetricAmbientProbeOutputBufferParam = Shader.PropertyToID("_VolumetricAmbientProbeOutputBuffer");
        static readonly int s_DiffuseAmbientProbeOutputBufferParam = Shader.PropertyToID("_DiffuseAmbientProbeOutputBuffer");
        static readonly int s_ScratchBufferParam = Shader.PropertyToID("_ScratchBuffer");
        static readonly int s_AmbientProbeInputCubemap = Shader.PropertyToID("_AmbientProbeInputCubemap");
        static readonly int s_FogParameters = Shader.PropertyToID("_FogParameters");

        public Cubemap skyCubemap;
        
        public ComputeShader computeAmbientProbeCS;
        public ComputeBuffer ambientProbeResult { get; private set; }
        public ComputeBuffer diffuseAmbientProbeBuffer { get; private set; }
        public ComputeBuffer volumetricAmbientProbeBuffer { get; private set; }
        public ComputeBuffer scratchBuffer { get; private set; }
        
        SphericalHarmonicsL2 m_AmbientProbe;
        
        internal bool ambientProbeIsReady = false;
        
        public int computeAmbientProbeKernel;

        private Vector3[] kCubemapOrthoBases = new[]
        {
            new Vector3(0, 0, -1), new Vector3(0, -1, 0), new Vector3(-1, 0, 0),
            new Vector3(0, 0, 1), new Vector3(0, -1, 0), new Vector3(1, 0, 0),
            new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, -1, 0),
            new Vector3(1, 0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0),
            new Vector3(1, 0, 0), new Vector3(0, -1, 0), new Vector3(0, 0, -1),
            new Vector3(-1, 0, 0), new Vector3(0, -1, 0), new Vector3(0, 0, 1),
        };

        public SkyUpdatePass(RenderPassEvent renderPassEvent, PostProcessData data)
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

        private RTHandle cubemapRT;
        
        public void Setup()
        {
            ClearAmbientProbe();
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var des = renderingData.cameraData.cameraTargetDescriptor;
            des.dimension = TextureDimension.Cube;
            des.width = 32;
            des.height = 32;
            des.depthBufferBits = 0;
            des.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(ref cubemapRT, des, FilterMode.Bilinear, TextureWrapMode.Clamp,
                name: "_AmbientSkyCube");
        
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;
            var camera = renderingData.cameraData.camera;
            
            // Render Sky Cube
            for (int i = 0; i < 6; ++i)
            {
            
                var viewMatrix = camera.worldToCameraMatrix;
                viewMatrix = viewMatrix.SetBasisTransposed(kCubemapOrthoBases[i * 3 + 0], kCubemapOrthoBases[i * 3 + 1], kCubemapOrthoBases[i * 3 + 2]);
                Matrix4x4 inverseTransformMat = Matrix4x4.identity;
                inverseTransformMat.SetTranslate(-camera.transform.position);
                viewMatrix *= inverseTransformMat;
                cmd.SetViewMatrix(viewMatrix);
                CoreUtils.SetRenderTarget(cmd, cubemapRT, ClearFlag.None, 0, (CubemapFace)i);
            
                context.ExecuteCommandBuffer(cmd);
                context.DrawSkybox(camera);
            }
            cmd.Clear();
            cmd.SetViewMatrix(renderingData.cameraData.GetViewMatrix());
            cmd.SetGlobalTexture("_AmbientSkyCube", cubemapRT);
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
