using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Lighting/RSM", typeof(UniversalRenderPipeline))]
    public class RSMVolume: VolumeComponent, IPostProcessComponent
    {
        public BoolParameter isEnable = new BoolParameter(false);

        public bool IsActive() => isEnable.value;

        public bool IsTileCompatible()
        { 
            throw new System.NotImplementedException();
        }
    }
}