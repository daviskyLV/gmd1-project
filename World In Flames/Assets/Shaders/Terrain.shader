Shader "Custom/Terrain"
{
    Properties
    {
        // some color settings
        _OceanColor ("Ocean Color", Color) = (1,1,1,1)
        _SeaColor ("Sea Color", Color) = (1,1,1,1)
        _DebugGroundColor ("Debug Ground Color", Color) = (1,1,1,1)
        // textures
        _GrassTex ("Grass Texture", 2D) = "white" {}
        _SnowTex ("Snow Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            //EASING FUNCTION ENUMS
            #define EaseLinear      0
            #define EaseInSine      1
            #define EaseOutSine     2
            #define EaseInOutSine   3
            #define EaseInCubic     4
            #define EaseOutCubic    5
            #define EaseInOutCubic  6
            #define EaseInQuadratic 7

            /// SETTINGS
            TEXTURE2D(_TerrainData); // R = height, G = temperature, B = humidity
            SAMPLER(sampler_TerrainData);
            float2 _MapSize; // Width, Height for Height & Humidity
            int _ProvinceResolution; // pixels per province
            float _SeaLevel; // sea level
            // textures
            TEXTURE2D(_GrassTex);
            SAMPLER(sampler_GrassTex);
            TEXTURE2D(_SnowTex);
            SAMPLER(sampler_SnowTex);
            // additional colors
            float4 _OceanColor;
            float4 _SeaColor;
            float4 _DebugGroundColor;

            struct VertexData
            {
                float4 positionOS : POSITION;
            };

            struct FragmentData
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            // Reimplementation from BurstUtilities
            float calculateEasing (float progress, int easing)
            {
                if (easing == EaseLinear) {
                    return progress;
                }
                else if (easing == EaseInQuadratic) {
                    return pow(progress, 4);
                }
                else if (easing == EaseInSine) {
                    return 1.0 - cos((progress * PI) / 2.0);
                }
                else if (easing == EaseOutSine) {
                    return sin((progress * PI) / 2.0);
                }
                else if (easing == EaseInOutSine) {
                    return -(cos(PI * progress) - 1.0) / 2.0;
                }
                else if (easing == EaseInCubic) {
                    return pow(progress, 3);
                }
                else if (easing == EaseOutCubic) {
                    return 1.0 - pow(1.0 - progress, 3);
                }
                else if (easing == EaseInOutCubic) {
                    if (progress < 0.5) {
                        return 4.0 * pow(progress, 3);
                    }
                    else {
                        1.0 - pow(-2.0 * progress + 2.0, 3) / 2.0;
                    }
                }

                return progress; // fallback
            }

            // mandatory for hlsl to pass data to pixel (fragment) function
            // even though we dont do anything with the vertices themselves
            FragmentData vert (VertexData IN)
            {
                FragmentData OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS); // to world space
                OUT.positionHCS = TransformWorldToHClip(positionWS.xyz);
                OUT.worldPos = positionWS.xyz;
                return OUT;
            }

            // actual pixel coloring method
            // same as surf in SurfaceShader (?)
            float4 frag (FragmentData IN) : SV_Target
            {
                // converting world position to coordinates
                float2 arrCoord = floor(IN.worldPos.xz * _ProvinceResolution);
                // since data is in a texture, we normalize to UV
                float2 uv = (arrCoord + 0.5) / _MapSize;
                uv = saturate(uv);

                // getting data, while humidity is _MapSize / _ProvinceResolution, it repeats data in gaps, so we are fine
                // sampler_TerrainData is Unity related stuff, idk
                float height = SAMPLE_TEXTURE2D(_TerrainData, sampler_TerrainData, uv).r;
                float temperature = SAMPLE_TEXTURE2D(_TerrainData, sampler_TerrainData, uv).g;
                float humidity = SAMPLE_TEXTURE2D(_TerrainData, sampler_TerrainData, uv).b;

                // below waterline, blending between ocean and sea colors
                if (height < _SeaLevel)
                {
                    return lerp(_OceanColor, _SeaColor, calculateEasing(saturate(height/_SeaLevel), EaseInQuadratic));
                }

                return _DebugGroundColor;
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}