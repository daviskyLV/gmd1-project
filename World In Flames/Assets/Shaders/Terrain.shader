Shader "Custom/Terrain"
{
    Properties
    {
        // some color settings
        [Header(Color Settings)]
        _OceanColor ("Ocean Color", Color) = (1,1,1,1)
        _SeaColor ("Sea Color", Color) = (1,1,1,1)
        _DebugGroundColor ("Debug Ground Color", Color) = (1,1,1,1)
        [Toggle] _DebugMode ("Debug Mode (water & ground)", Float) = 0
        // textures
        [Header(Textures)]
        _GrassTex ("Grass Texture", 2D) = "white" {}
        _GrassTexScale ("Grass Texture Scale", Float) = 1
        _SnowTex ("Snow Texture", 2D) = "white" {}
        _SnowTexScale ("Snow Texture Scale", Float) = 1
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // EASING FUNCTION ENUMS
            #define EaseLinear      0
            #define EaseInSine      1
            #define EaseOutSine     2
            #define EaseInOutSine   3
            #define EaseInCubic     4
            #define EaseOutCubic    5
            #define EaseInOutCubic  6
            #define EaseInQuart     7
            // TEXTURE ENUMS
            #define GrassTexIndex   0
            #define SnowTexIndex    1

            /// SETTINGS
            TEXTURE2D(_TerrainData); // R = height, G = temperature, B = humidity
            SAMPLER(sampler_TerrainData);
            float2 _MapSize; // Width, Height for Height & Humidity
            int _ProvinceResolution; // pixels per province
            float _SeaLevel; // sea level
            float _DebugMode; // if true, no textures will be used 
            // textures
            TEXTURE2D(_GrassTex);
            SAMPLER(sampler_GrassTex);
            float _GrassTexScale;
            TEXTURE2D(_SnowTex);
            SAMPLER(sampler_SnowTex);
            float _SnowTexScale;
            // additional colors
            float4 _OceanColor;
            float4 _SeaColor;
            float4 _DebugGroundColor;

            struct VertexData
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct FragmentData
            {
                float4 positionHCS : SV_POSITION;
                float3 newWorldPos : TEXCOORD0; // modified (eg. waves) world position
                float3 realObjPos : TEXCOORD1; // real object pos
                float3 newObjPos : TEXCOORD2; // new object pos
                float3 normalWS : TEXCOORD3; // normal world space
                float4 shadowCoord : TEXCOORD4;
            };

            // Reimplementation from BurstUtilities
            float calculateEasing (float progress, int easing)
            {
                if (easing == EaseLinear) {
                    return progress;
                }
                else if (easing == EaseInQuart) {
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
                VertexPositionInputs realVxInput = GetVertexPositionInputs(IN.positionOS);
                float3 newObjPos = float3(IN.positionOS.x, max(IN.positionOS.y, _SeaLevel), IN.positionOS.z); // flattening sea vertices
                VertexPositionInputs newVxInput = GetVertexPositionInputs(newObjPos);

                OUT.positionHCS = newVxInput.positionCS; // passing the new vertex position data to screen
                OUT.newWorldPos = newVxInput.positionWS;
                OUT.realObjPos = IN.positionOS;
                OUT.newObjPos = newObjPos;
                OUT.normalWS = TransformObjectToWorldDir(IN.normalOS);
                OUT.shadowCoord = GetShadowCoord(newVxInput);
                return OUT;
            }

            // code by Sebastian Lague adapted from Surface Shader to HLSL Shader
            // https://youtu.be/XjH-UoyaTgs?si=di0VP-33FN3jvF0R&t=1448
            float3 sampleTexture(float2 pos, int textureIndex) {
                uint width, height;
                if (textureIndex == GrassTexIndex) {
                    _GrassTex.GetDimensions(width, height);
                    float2 wh = float2(width, height);
                    float2 uv = saturate((pos * _GrassTexScale) % wh / wh);
                    return SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, uv).rgb;
                } else if (textureIndex == SnowTexIndex) {
                    _SnowTex.GetDimensions(width, height);
                    float2 wh = float2(width, height);
                    float2 uv = saturate((pos * _SnowTexScale) % wh / wh);
                    return SAMPLE_TEXTURE2D(_SnowTex, sampler_SnowTex, uv).rgb;
                }
                // fallback
                return float3(pos.x % 2, pos.y % 2, 1);
            }

            float3 triplanar (float3 worldPos, float3 blendAxes, int texIndex) {
                float3 xProjection = sampleTexture(worldPos.yz, texIndex) * blendAxes.x;
                float3 yProjection = sampleTexture(worldPos.xz, texIndex) * blendAxes.y;
                float3 zProjection = sampleTexture(worldPos.xy, texIndex) * blendAxes.z;
                return xProjection + yProjection + zProjection;
            }

            // actual pixel coloring method
            // same as surf in SurfaceShader (?)
            float4 frag (FragmentData IN) : SV_Target
            {
                // getting main light data
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 lightColor = mainLight.color;
                float shadowAttenuation = mainLight.shadowAttenuation; // 0 = fully shadowed, 1 = lit
                
                //Lambert Diffuse Lighting
                float3 normal = normalize(IN.normalWS);
                float3 lightDir = mainLight.direction;
                float NdotL = max(dot(normal, lightDir), 0);

                // converting world position to coordinates
                float2 arrCoord = IN.newWorldPos.xz * _ProvinceResolution;
                // since data is in a texture, we normalize to UV
                float2 uv = (arrCoord + 0.5) / _MapSize;
                uv = saturate(uv);

                // getting data, while humidity is _MapSize / _ProvinceResolution, it repeats data in gaps, so we are fine
                // sampler_TerrainData is Unity related stuff, idk
                float height = SAMPLE_TEXTURE2D(_TerrainData, sampler_TerrainData, uv).r;
                float temperature = SAMPLE_TEXTURE2D(_TerrainData, sampler_TerrainData, uv).g;
                float humidity = SAMPLE_TEXTURE2D(_TerrainData, sampler_TerrainData, uv).b;

                float4 terrainColor = float4(IN.newWorldPos.x % 2, IN.newWorldPos.z % 2, 1, 1);
                // below waterline, blending between ocean and sea colors
                if (IN.realObjPos.y < _SeaLevel)
                {
                    // couldnt get correctly working results with height, so using world space
                    terrainColor = lerp(_OceanColor, _SeaColor, calculateEasing(saturate(IN.realObjPos.y/_SeaLevel), EaseInQuart));
                } else {
                    // above waterline, showing ground Textures
                    // code by Sebastian Lague adapted from Surface Shader to HLSL Shader
                    // https://youtu.be/XjH-UoyaTgs?si=di0VP-33FN3jvF0R&t=1448
                    if (_DebugMode > 0.5) {
                        // debug mode active, showing debug ground color
                        terrainColor = _DebugGroundColor;
                    } else {
                        float3 blendAxes = abs(IN.normalWS);
                        blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
                        //terrainColor.rgb = triplanar(IN.worldPos, blendAxes, GrassTexIndex); // grass
                        if (temperature < 0.2) {
                            terrainColor.rgb = triplanar(IN.newWorldPos, blendAxes, SnowTexIndex); //snow
                        } else {
                            terrainColor.rgb = triplanar(IN.newWorldPos, blendAxes, GrassTexIndex); //grass
                        }
                    }
                }

                // applying lighting
                float3 ambient = terrainColor.rgb * unity_AmbientSky.rgb;
                float3 diffuse = terrainColor.rgb * lightColor * NdotL * shadowAttenuation;

                return float4(ambient + diffuse, 1.0);
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}