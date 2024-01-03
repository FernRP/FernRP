#ifndef UNIVERSAL_SURFACE_DATA_INCLUDED
#define UNIVERSAL_SURFACE_DATA_INCLUDED

#if _NPR
    #include "SurfaceDataNPR.hlsl"
#elif _MIRCOGARIN
    #include "SurfaceDataMicroGrain.hlsl"
#else
    // Must match Universal ShaderGraph master node
    struct SurfaceData
    {
        half3 albedo;
        half3 specular;
        half  metallic;
        half  smoothness;
        half3 normalTS;
        half3 emission;
        half  occlusion;
        half  alpha;
        half  clearCoatMask;
        half  clearCoatSmoothness;
    };

#endif


#endif
