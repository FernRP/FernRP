using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
     [Serializable, VolumeComponentMenuForRenderPipeline("FernRP/Lighting/RayTracing", typeof(UniversalRenderPipeline))]
    public class RayTracingVolume : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter isEnable = new BoolParameter(false);

        public bool IsActive() => isEnable.value;

        public bool IsTileCompatible()
        {
            return false;
        }
    }

}

