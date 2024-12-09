using System;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.FernRenderPipeline
{
    [Serializable, VolumeComponentMenuForRenderPipeline("FernRP/Lighting/Ambient Probe Update", typeof(UniversalRenderPipeline))]
    public class AmbientProbeUpdateVolume: VolumeComponent, IPostProcessComponent
    {
        public BoolParameter isEnable = new BoolParameter(false);

        public bool IsActive()
        {
            return isEnable.overrideState && isEnable.value;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}