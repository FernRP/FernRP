using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Lighting/RSM", typeof(UniversalRenderPipeline))]
    public class RSMVolume: VolumeComponent, IPostProcessComponent
    {
        
        public BoolParameter isEnable = new BoolParameter(false);
        public BoolParameter OnlyAdditionalLight = new BoolParameter(false);
        public FloatParameter RSMSampleCount = new FloatParameter(32);
        public FloatParameter RSMIntensity = new FloatParameter(1);

        public bool IsActive() => isEnable.value;

        public bool IsTileCompatible()
        { 
            throw new System.NotImplementedException();
        }
    }
}