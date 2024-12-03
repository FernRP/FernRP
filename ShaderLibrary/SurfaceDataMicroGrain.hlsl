#ifndef UNIVERSAL_SURFACE_DATA_INCLUDED
#define UNIVERSAL_SURFACE_DATA_INCLUDED

struct SurfaceData
{
    half3 albedo;
    half3 specular;
    half3 normalTS;
    half3 emission;
    
    half  metallic;
    half  smoothness;
    half  occlusion;
    half  alpha;
    half  diffuseID;
    half  innerLine;

    half  clearCoatMask;
    half  clearCoatSmoothness;
    
    half3 porousColor;
    half  porousDensity;
    half  porousSmoothness;
    half  porousMetallic;
};


#endif
