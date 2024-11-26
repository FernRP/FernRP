using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.FernRenderPipeline;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.FernRenderPipeline
{
    public class FernRPMenu : MonoBehaviour
    {
        [MenuItem("GameObject/Rendering/FernRP", false, 100)]
        public static void CreateFernRP()
        {
            var fernRP = GameObject.FindObjectOfType<FernRP>();
            if (fernRP == null)
            {
                var fernRenderGameObject = new GameObject();
                fernRenderGameObject.name = "FernRP";
                var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
                fernRP = fernRenderGameObject.AddComponent<FernRP>();
                fernRP.renderPipelineAsset = renderPipelineAsset;
                GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
            }
            
            Selection.activeObject = fernRP.gameObject;
        } 
        
        [MenuItem("GameObject/Light/Fern Reflection Probe", false, 99)]
        public static void CreateFernReflectionProbe()
        {
            var fernReflection = GameObject.FindObjectOfType<FernAdditionalReflectionProbe>();
            if (fernReflection == null)
            {
                var fernReflectionProbeGo = new GameObject();
                fernReflectionProbeGo.name = "Fern Reflection Probe";
                var reflection = fernReflectionProbeGo.AddComponent<ReflectionProbe>();
                fernReflection = fernReflectionProbeGo.AddComponent<FernAdditionalReflectionProbe>();
            }
            
            Selection.activeObject = fernReflection.gameObject;
        }
    }
}