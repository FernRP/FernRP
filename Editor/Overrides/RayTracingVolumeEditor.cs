using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    
    [CustomEditor(typeof(RayTracingVolume))]
    public class RayTracingVolumeEditor : VolumeComponentEditor
    {
        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledGroupScope(serializedObject.isEditingMultipleObjects))
            {
                if (GUILayout.Button("Setup RayTracing Data", EditorStyles.miniButton))
                {
                    // TODO: Start building BVH 
                }
            }
        }
    }
}