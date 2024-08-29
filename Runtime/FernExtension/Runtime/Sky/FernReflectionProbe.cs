using System;

namespace UnityEngine.Rendering.Universal
{
    [RequireComponent(typeof(ReflectionProbe))]
    [ExecuteAlways]
    public class FernReflectionProbe : MonoBehaviour
    {
        private ReflectionProbe reflectionProbe;
        private static readonly int AmbientSkyCube = Shader.PropertyToID("_AmbientSkyCube");

        private void OnEnable()
        {
            reflectionProbe = GetComponent<ReflectionProbe>();
            reflectionProbe.mode = ReflectionProbeMode.Realtime;
            reflectionProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            reflectionProbe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            reflectionProbe.cullingMask = (LayerMask)0;
            reflectionProbe.clearFlags = ReflectionProbeClearFlags.Skybox;
        }

        private void LateUpdate()
        {
            reflectionProbe.mode = ReflectionProbeMode.Realtime;
            reflectionProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;

            reflectionProbe.RenderProbe();
            
            Shader.SetGlobalTexture(AmbientSkyCube, reflectionProbe.texture);
        }
    }
}