Shader "FernRP/URP/FERNMicroGarin"
{
    Properties
    {
        [Main(ShadingMap, _, off, off)]
        _groupShadingMask ("Shading Map", float) = 0
        [Space()]
        [Tex(ShadingMap)] _ShadingMap01 ("Shading Mask Map 1", 2D) = "white" { }
        [Sub(ShadingMap)] _ShadingMap01_ST ("Shading Mask Map 1 ST", vector) = (1,1,0,0)
        [Tex(ShadingMap)] _LightMap ("PBR Light Map", 2D) = "white" { }
        
        [Main(Surface, _, off, off)]
        _group ("Base Layer", float) = 0
        [Space()]
        [Tex(Surface, _BaseColor)] _BaseMap ("Base Map", 2D) = "white" { }
        [Sub(Surface)] _BaseMap_ST ("Base Map Tiling", vector) = (1,1,0,0)
        [HideInInspector] _BaseColor ("Base Color", color) = (0.5, 0.5, 0.5, 1)
        [SubToggle(Surface, _NORMALMAP)] _BumpMapKeyword("Use Normal Map", Float) = 0.0
        [Tex(Surface_NORMALMAP)] _BumpMap ("Normal Map", 2D) = "bump" { }
        [Sub(Surface_NORMALMAP)] _BumpScale("Scale", Float) = 1.0
        
        [Channel(Surface)] _PBRMetallicChannel("Metallic Channel", Vector) = (1,0,0,0)
        [Sub(Surface)] _Metallic("Metallic", Range(0, 1.0)) = 0.0
        [Channel(Surface)] _PBRSmothnessChannel("Smoothness Channel", Vector) = (0,0,0,1)
        [Sub(Surface)] _Smoothness("Base Smoothness", Range(0, 1.0)) = 0.5 
        [Channel(Surface)] _PBROcclusionChannel("Occlusion Channel", Vector) = (0,1,0,0)
        [Sub(Surface)] _OcclusionStrength("Occlusion Strength", Range(0, 1.0)) = 0.0
        
        [Main(PorousLayer, _, off, off)]
        _groupPorousLayer ("Porous Layer", float) = 0
        [Space()]
        [Tex(PorousLayer)] _PorousMap ("Porous Color Map", 2D) = "white" { }
        [Sub(PorousLayer)] _PorousMap_ST ("PorousMap Tiling", vector) = (1,1,0,0)
        [Sub(PorousLayer)] _PorousColor ("Porous Color", Color) = (0.5,0.5,0.5,1)
        [Sub(PorousLayer)] _PorousDensity("Porous Density", Range(0,1)) = 0.4
        [Channel(PorousLayer)] _PorousDensityChannel("Porous Density Channel", Vector) = (0,0,0,1)
        [Sub(PorousLayer)] _PorousMetallic("Metallic", Range(0, 1.0)) = 0.0
        [Sub(PorousLayer)] _PorousSmoothness("Porous Smoothness", Range(0, 1.0)) = 0.5
        
        [Main(Environment, _, off, off)]
        _groupEnvironment ("EnvironmentSettings", float) = 1
        [Space()]
        [KWEnum(Environment, None, _, RenderSetting, _RENDERENVSETTING, CustomCube, _CUSTOMENVCUBE)] _enum_env ("Environment Source", float) = 1
        
        [Main(EmssionSetting, _, off, off)]
        _groupEmission ("Emission Setting", float) = 0
        [Space()]
        [SubToggle(EmssionSetting, _USEEMISSIONTEX)] _UseEmissionTex("Use Emission Tex", Float) = 0.0
        [Tex(EmssionSetting_USEEMISSIONTEX)] _EmissionTex ("Emission Tex", 2D) = "white" { }
        [Channel(EmssionSetting)] _EmissionChannel("Emission Channel", Vector) = (0,0,1,0)
        [Sub(EmssionSetting)] [HDR]_EmissionColor("Emission Color", Color) = (0,0,0,0)
        [Sub(EmssionSetting)] _EmissionColorAlbedoWeight("Emission Color Albedo Weight", Range(0, 1)) = 0
        
        [Main(Rim, _, off, off)]
        _groupRim ("RimSettings", float) = 1
        [Space()]
        [KWEnum(Rim, None, _, FresnelRim, _FRESNELRIM, ScreenSpaceRim, _SCREENSPACERIM)] _enum_rim ("Rim Mode", float) = 0
        [Sub(Rim)] [ShowIf(_enum_rim, NEqual, 0)] _RimDirectionLightContribution("Directional Light Contribution", Range(0,1)) = 1.0
        [Sub(Rim)] [ShowIf(_enum_rim, NEqual, 0)] [HDR] _RimColor("Rim Color",Color) = (1,1,1,1)
        [Sub(Rim)] [ShowIf(_enum_rim, Equal, 1)] _RimThreshold("Rim Threshold",Range(0,1)) = 0.2
        [Sub(Rim)] [ShowIf(_enum_rim, Equal, 1)] _RimSoftness("Rim Softness",Range(0.001,1)) = 0.01
        [SubToggle(Rim)] [ShowIf(_enum_rim, Equal, 2)] _DepthOffsetRimReverseX("Depth Offset Reverse X", Float) = 0
        [Sub(Rim)] [ShowIf(_enum_rim, Equal, 2)] _DepthRimOffset("Depth Rim Width",Range(-32,32)) = 0.01
        [Sub(Rim)] [ShowIf(_enum_rim, Equal, 2)] _DepthRimThresoldOffset("Depth Rim Thresold Offset",Range(0,32)) = 0.01
        
        [Main(ShadowSetting, _, off, off)]
        _groupShadowSetting ("Shadow Setting", float) = 1
        [Space()]
        [SubToggleOff(ShadowSetting, _RECEIVE_SHADOWS_OFF)] _RECEIVE_SHADOWS_OFF("RECEIVE_SHADOWS", Float) = 1
        
        [Main(AdditionalLightSetting, _, off, off)]
        _groupAdditionLight ("AdditionalLightSetting", float) = 1
        [Space()]
        [SubToggle(AdditionalLightSetting)] _Is_Filter_LightColor("Is Filter LightColor", Float) = 1
        [Sub(AdditionalLightSetting)] _LightIntensityClamp("Additional Light Intensity Clamp", Range(0, 8)) = 1

        [Main(Outline, _, off, off)]
        _groupOutline ("OutlineSettings", float) = 1
        [Space()]
        [SubToggle(Outline, _OUTLINE)] _Outline("Use Outline", Float) = 0.0
        [Sub(Outline_OUTLINE)] _OutlineColor ("Outline Color", Color) = (0,0,0,0)
        [Sub(Outline_OUTLINE)] _OutlineWidth ("Outline Width", Range(0, 10)) = 1
        [KWEnum(Outline, None, _, UV8.RG, _SMOOTHEDNORMAL)] _enum_outline_smoothed("Smoothed Normal", float) = 0
        [KWEnum(Outline, None, _, VertexColor.A, _OUTLINEWIDTHWITHVERTEXTCOLORA, UV8.A, _OUTLINEWIDTHWITHUV8A)] _enum_outline_width("Override Outline Width", float) = 0
        [KWEnum(Outline, None, _, BaseMap, _OUTLINECOLORBLENDBASEMAP, VertexColor, _OUTLINECOLORBLENDVERTEXCOLOR)] _enum_outline_color("Blend Outline Color", float) = 0

        // RenderSetting
        [Title(_, RenderSetting)]
        [Main(RenderSetting, _, off, off)]
        _groupSurface ("RenderSetting", float) = 1
        [Surface(RenderSetting)] _Surface("Surface Type", Float) = 0.0
        [SubEnum(RenderSetting, UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2.0
        [SubEnum(RenderSetting, UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Alpha", Float) = 1.0
        [SubEnum(RenderSetting, UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Alpha", Float) = 0.0
        [SubEnum(RenderSetting, Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1.0
        [SubEnum(RenderSetting, Off, 0, On, 1)] _DepthPrePass("Depth PrePass", Float) = 0
        [SubEnum(RenderSetting, Off, 0, On, 1)] _CasterShadow("Caster Shadow", Float) = 1
        [Sub(RenderSetting)]_Cutoff("Alpha Clipping", Range(0.0, 1.0)) = 0.5
        [Sub(RenderSetting)]_ZOffset("Z Offset", Range(-1.0, 1.0)) = 0
        [Queue(RenderSetting)] _QueueOffset("Queue offset", Range(-50, 50)) = 0.0
    }
    
    HLSLINCLUDE
    	#define _MIRCOGARIN 1
    ENDHLSL
    
    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "NPRLit" "IgnoreProjector" = "True"}
        LOD 300

        // TODO: 
//        Pass
//        {
//            Name "FernDepthPrePass"
//            Tags{"LightMode" = "SRPDefaultUnlit"} // Hard Code Now
//
//            Blend Off
//            ZWrite on
//            Cull off
//            ColorMask 0
//
//            HLSLPROGRAM
//            #pragma only_renderers gles gles3 glcore d3d11
//            #pragma target 3.0
//
//            #pragma vertex LitPassVertex
//            #pragma fragment LitPassFragment_DepthPrePass
//
//            #include "MicroGrainInput.hlsl"
//            #include "FernStandardForwardPass.hlsl"
//            ENDHLSL
//        }
        
        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma target 3.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _DEPTHSHADOW
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _SPECULARAA
            #pragma shader_feature_local _SPECULARMASK
            #pragma shader_feature_local _ _FRESNELRIM _SCREENSPACERIM
            #pragma shader_feature_local _ _RENDERENVSETTING _CUSTOMENVCUBE
            #pragma shader_feature_local _MATCAP
            #pragma shader_feature_local _USEEMISSIONTEX

            // -------------------------------------
            // Fern Keywords

            // -------------------------------------
            // Effect Keyword
            
            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "MicroGrainInput.hlsl"
            #include "FernStandardForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma target 3.0

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            // -------------------------------------
            // Universal Pipeline keywords

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "MicroGrainInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/FernShaderLibrary/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        
        // TODO: This is no finish...
//        Pass
//        {
//            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
//            // no LightMode tag are also rendered by Universal Render Pipeline
//            
//            Name "GBuffer"
//            Tags{"LightMode" = "UniversalGBuffer"}
//
//            ZWrite[_ZWrite]
//            ZTest LEqual
//            Cull[_Cull]
//
//            HLSLPROGRAM
//            #pragma exclude_renderers gles gles3 glcore
//            #pragma target 4.5
//
//            // -------------------------------------
//            // Material Keywords
//            #pragma shader_feature_local _NORMALMAP
//            #pragma shader_feature_local_fragment _ALPHATEST_ON
//            //#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
//            #pragma shader_feature_local_fragment _EMISSION
//            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
//            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
//            #pragma shader_feature_local_fragment _OCCLUSIONMAP
//            #pragma shader_feature_local _PARALLAXMAP
//            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
//
//            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
//            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
//            #pragma shader_feature_local_fragment _SPECULAR_SETUP
//            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
//
//            // -------------------------------------
//            // Universal Pipeline keywords
//            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
//            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
//            //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
//            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
//            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
//            #pragma multi_compile_fragment _ _SHADOWS_SOFT
//            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
//            #pragma multi_compile_fragment _ _LIGHT_LAYERS
//            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
//
//            // -------------------------------------
//            // Unity defined keywords
//            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
//            #pragma multi_compile _ SHADOWS_SHADOWMASK
//            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
//            #pragma multi_compile _ LIGHTMAP_ON
//            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
//            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
//
//            //--------------------------------------
//            // GPU Instancing
//            #pragma multi_compile_instancing
//            #pragma instancing_options renderinglayer
//            #pragma multi_compile _ DOTS_INSTANCING_ON
//
//            #pragma vertex LitGBufferPassVertex
//            #pragma fragment LitGBufferPassFragment
//
//            #include "MicroGrainInput.hlsl"
//            #include "NPRStandardGBufferPass.hlsl"
//            ENDHLSL
//        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma target 3.0

            // -------------------------------------
            // Fern Keywords
            #pragma shader_feature_local_vertex _PERSPECTIVEREMOVE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #include "MicroGrainInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/FernShaderLibrary/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma target 3.0

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Fern Keywords
            #pragma shader_feature_local_vertex _PERSPECTIVEREMOVE

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            // Universal Pipeline keywords
            #pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "MicroGrainInput.hlsl"
            #include "NPRDepthNormalsPass.hlsl"
            ENDHLSL
        }
        
        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags{"LightMode" = "Meta"}

            Cull Off

            HLSLPROGRAM
            #pragma only_renderers gles gles3 glcore d3d11
            #pragma target 3.0

            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit

            #pragma shader_feature EDITOR_VISUALIZATION
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            
            #pragma shader_feature_local_fragment _USEDISSOLVEEFFECT

            #pragma shader_feature_local_fragment _SPECGLOSSMAP

            #include "MicroGrainInput.hlsl"
            #include "NPRMetaPass.hlsl"

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "LWGUI.LWGUI"
}
