Shader "Custom/PostProcess/CircleCutout"
{
    Properties
    {
        [Header(Circle Cutout)]
        _CutoutRadius ("Cutout Radius", Range(0, 50)) = 3
        _CutoutFadeWidth ("Fade Width", Range(0, 20)) = 1
        _PlayerWorldPos ("Player World Pos", Vector) = (0, 0, 0, 0)

        [Header(Source Select)]
        [Toggle] _UseGlobalParams ("Use Global Params", Float) = 1

        [Header(Debug)]
        [KeywordEnum(OFF, DEPTH_RAW, BACKGROUND, WORLD_POS)] _Debug ("Debug View", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "CircleCutoutPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_LinearClamp);

            TEXTURE2D_X(_CircleCutoutBackground);

            TEXTURE2D_X_FLOAT(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            float4 _PlayerWorldPos;
            float  _CutoutRadius;
            float  _CutoutFadeWidth;
            float  _UseGlobalParams;
            float  _Debug;

            float4 _Global_PlayerWorldPos;
            float  _Global_CutoutRadius;
            float  _Global_CutoutFadeWidth;

            struct VOut
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            VOut Vert(uint vertexID : SV_VertexID)
            {
                VOut o;
                o.positionHCS = GetFullScreenTriangleVertexPosition(vertexID);
                o.uv          = GetFullScreenTriangleTexCoord(vertexID);
                return o;
            }

            half4 Frag(VOut i) : SV_Target
            {
                float2 uv = i.uv;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half4 background = SAMPLE_TEXTURE2D_X(_CircleCutoutBackground, sampler_LinearClamp, uv);

                // Select params
                float4 playerPos = _UseGlobalParams > 0.5 ? _Global_PlayerWorldPos : _PlayerWorldPos;
                float  radius    = _UseGlobalParams > 0.5 ? _Global_CutoutRadius : _CutoutRadius;
                float  fadeWidth = _UseGlobalParams > 0.5 ? _Global_CutoutFadeWidth : _CutoutFadeWidth;

                // Debug views
                if (_Debug > 0.5 && _Debug < 1.5)
                {
                    float d = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                    return half4(d, d, d, 1.0);
                }
                if (_Debug > 1.5 && _Debug < 2.5)
                {
                    return background;
                }

                // Passthrough when disabled
                if (radius + fadeWidth <= 0.0)
                    return color;

                // Sample depth
                float rawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;

                // Skip skybox - keep full scene
                #if UNITY_REVERSED_Z
                    if (rawDepth < 0.0001) return color;
                #else
                    if (rawDepth > 0.9999) return color;
                #endif

                // Reconstruct world position
                #if UNITY_REVERSED_Z
                    float depthNDC = rawDepth;
                #else
                    float depthNDC = rawDepth * 2.0 - 1.0;
                #endif

                float2 posCS = uv * 2.0 - 1.0;
                float4 clipPos = float4(posCS, depthNDC, 1.0);
                float4 worldPos4 = mul(UNITY_MATRIX_I_VP, clipPos);
                float3 worldPos = worldPos4.xyz / worldPos4.w;

                // Debug: world pos (R=X, G=Y, B=Z normalized)
                if (_Debug > 2.5)
                {
                    float3 relPos = (worldPos - playerPos.xyz) / 20.0 + 0.5;
                    return half4(saturate(relPos), 1.0);
                }

                // XZ distance to player
                float dist = length(worldPos.xz - playerPos.xz);

                // Cutout blend
                // t=0 (inside radius) -> background (Wall hidden)
                // t=1 (outside radius) -> color (Wall visible)
                float t = saturate((dist - radius) / max(fadeWidth, 0.001));
                return lerp(background, color, t);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
