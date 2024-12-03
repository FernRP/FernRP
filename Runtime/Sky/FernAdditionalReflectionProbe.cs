using System;

namespace UnityEngine.Rendering.FernRenderPipeline
{
    [RequireComponent(typeof(ReflectionProbe))]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class FernAdditionalReflectionProbe : MonoBehaviour
    {
        [SerializeField]
        private bool m_RealtimeAmbient = false;

        public bool realtimeAmbient
        {
            get => m_RealtimeAmbient;
            set => m_RealtimeAmbient = value;
        }
    }
    
    public static class ReflectionProbeExtension{
        public static FernAdditionalReflectionProbe GetAdditionalReflectionProbe(this ReflectionProbe probe)
        {
            var gameObject = probe.gameObject;
            bool componentExists = gameObject.TryGetComponent<FernAdditionalReflectionProbe>(out var fernProbe);
            if (!componentExists)
                fernProbe = gameObject.AddComponent<FernAdditionalReflectionProbe>();

            return fernProbe;
        }
    }
}