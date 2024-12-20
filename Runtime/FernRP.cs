using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.FernRenderPipeline
{
    [ExecuteAlways]
    public class FernRP : MonoBehaviour
    {
        public RenderPipelineAsset renderPipelineAsset;
        
        private UniversalAdditionalCameraData FernCameraData;
        private Vector4 depthSourceSize = Vector4.one;
        private float cameraAspect = 0;
        private float cameraFov = 0;
        
        private static readonly int ShaderID_DepthTextureSourceSize = Shader.PropertyToID("_DepthTextureSourceSize");
        private static readonly int ShaderID_CameraAspect = Shader.PropertyToID("_CameraAspect");
        private static readonly int ShaderID_CameraFOV = Shader.PropertyToID("_CameraFOV");

        private static FernRP instance;
        public static FernRP Get => instance != null ? instance : null;

        private void OnEnable()
        {
            instance = this;
            GraphicsSettings.defaultRenderPipeline = renderPipelineAsset;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRender;
            
            // Grass Enable
            OnEnableGrass();
        }

        private void OnDisable()
        {
            instance = null;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRender;
            
            // Grass Disable
            OnDisableGrass();
        }

        private void Update()
        {
            // Grass Update
            UpdateGrass();
        }

        private void OnBeginCameraRender(ScriptableRenderContext context, Camera currentCamera)
        {
            if (Math.Abs(cameraAspect - currentCamera.aspect) > 1e-5)
            {
                var aspect = currentCamera.aspect;
                cameraAspect = aspect;
                Shader.SetGlobalFloat(ShaderID_CameraAspect, 1.0f / aspect);
            }
    
            if (Math.Abs(cameraFov - currentCamera.fieldOfView) > 1e-5)
            {
                cameraFov = currentCamera.fieldOfView;
                Shader.SetGlobalFloat(ShaderID_CameraFOV, 1.0f / (currentCamera.orthographic? currentCamera.orthographicSize * 100 : currentCamera.fieldOfView));
            }

            // TODO: should get all cameras and then set sourceSize individually before camera rendering
            if (!depthSourceSize.z.Equals(currentCamera.pixelWidth) || !depthSourceSize.w.Equals(currentCamera.pixelHeight))
            {
                depthSourceSize.x = 1.0f / currentCamera.pixelWidth;
                depthSourceSize.y = 1.0f / currentCamera.pixelHeight;
                depthSourceSize.z = currentCamera.pixelWidth;
                depthSourceSize.w = currentCamera.pixelHeight;
                Shader.SetGlobalVector(ShaderID_DepthTextureSourceSize, depthSourceSize);
            }
        }

        public void ApplyRenderPipeline()
        {
            GraphicsSettings.defaultRenderPipeline = renderPipelineAsset;
        }
        
#if FERN_ENV_GRASS

        public FernGrassRender fernGrassRender;
        private void OnEnableGrass()
        {
            if(fernGrassRender == null) return;
            fernGrassRender.OnEnableGrass();
        }

        private void OnDisableGrass()
        {           
            if(fernGrassRender == null) return;
            fernGrassRender.OnDisableGrass();
        }
        
        private void UpdateGrass()
        {
            if(fernGrassRender == null) return;
            fernGrassRender.UpdateGrass();
        }
#else
        private void OnEnableGrass()
        {
            
        }

        private void OnDisableGrass()
        {
            
        }
        
        private void UpdateGrass()
        {
           
        }
#endif
    }
}

