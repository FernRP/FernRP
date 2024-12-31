using System;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.FernRenderPipeline
{
    /// <summary>
    /// Class containing shader and texture resources needed for Post Processing in URP.
    /// </summary>
    /// <seealso cref="Shader"/>
    /// <seealso cref="Texture"/>
    [Serializable]
    public class FernRPData : ScriptableObject
    {
#if UNITY_EDITOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreatePostProcessDataAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<FernRPData>();
                AssetDatabase.CreateAsset(instance, pathName);
                ResourceReloader.ReloadAllNullIn(instance, FernRenderPipelineAsset.packagePath);
                Selection.activeObject = instance;
            }
        }

        [MenuItem("Assets/Create/Rendering/Fern Render Pipeline Data", priority = CoreUtils.Sections.section5 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
        static void CreatePostProcessData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreatePostProcessDataAsset>(), "CustomFernRPData.asset", null, null);
        }

        internal static FernRPData GetDefaultPostProcessData()
        {
            var path = System.IO.Path.Combine(FernRenderPipelineAsset.packagePath, "Runtime/Data/FernRPData.asset");
            return AssetDatabase.LoadAssetAtPath<FernRPData>(path);
        }

#endif

        /// <summary>
        /// Class containing shader resources used for Post Processing in URP.
        /// </summary>
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            
            [Reload("Shaders/PostProcessing/EdgeDetectionOutline.shader")]
            public Shader edgeDetectionOutlinePS;
            
            [Reload("Shaders/Ambient/SHConvolution.compute")]
            public ComputeShader shConvolutionCS;
        }

        /// <summary>
        /// Shader resources used for Post Processing in URP.
        /// </summary>
        public ShaderResources shaders;
    }
}