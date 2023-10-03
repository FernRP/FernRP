using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FernRender
{
    public class FernRenderMenu : MonoBehaviour
    {
        [MenuItem("GameObject/Rendering/Fern Render", false, 100)]
        public static void CreateFernRender()
        {
            var fernRenderer = GameObject.FindObjectOfType<FernRenderer>();
            if (fernRenderer == null)
            {
                var fernRenderGameObject = new GameObject();
                fernRenderGameObject.name = "Fern Renderer";
                var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
                fernRenderer = fernRenderGameObject.AddComponent<FernRenderer>();
                fernRenderer.renderPipelineAsset = renderPipelineAsset;
                GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
            }
            
            Selection.activeObject = fernRenderer.gameObject;
        }
    }
}