Shader "Custom/PostProcess/CircleCutout"
{
    Properties
    {
        [Header(Circle Cutout)]
        _CutoutRadius ("Cutout Radius", Range(0, 50)) = 3
        _CutoutFadeWidth ("Fade Width", Range(0, 20)) = 1
        _HeightOffset ("Height Offset", Range(-5, 10)) = 0.5
        _PlayerWorldPos ("Player World Pos", Vector) = (0, 0, 0, 0)

        [Header(Source Select)]
        [Toggle] _UseGlobalParams ("Use Global Params", Float) = 1

        [Header(Debug)]
        [KeywordEnum(OFF, RAW_DEPTH, WORLD_Y)] _Debug ("Debug View", Float) = 0
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_LinearClamp);

            // Material properties
            float4 _PlayerWorldPos;
            float  _CutoutRadius;
            float  _CutoutFadeWidth;
            float  _HeightOffset;
            float  _UseGlobalParams;
            float  _Debug;

            // Global properties (from CircleCutoutController)
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

                // Select params
                float4 playerPos = _UseGlobalParams > 0.5 ? _Global_PlayerWorldPos : _PlayerWorldPos;
                float  radius    = _UseGlobalParams > 0.5 ? _Global_CutoutRadius : _CutoutRadius;
                float  fadeWidth = _UseGlobalParams > 0.5 ? _Global_CutoutFadeWidth : _CutoutFadeWidth;

                // Sample depth
                float rawDepth = SampleSceneDepth(uv);

                // Debug: raw depth
                if (_Debug > 0.5 && _Debug < 1.5)
                {
                    #if UNITY_REVERSED_Z
                        float vis = 1.0 - rawDepth;
                    #else
                        float vis = rawDepth;
                    #endif
                    return half4(vis, vis, vis, 1.0);
                }

                // Passthrough when disabled
                if (radius + fadeWidth <= 0.0)
                    return color;

                // Skip skybox
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

                // Debug: world Y
                if (_Debug > 1.5)
                {
                    float yNorm = saturate((worldPos.y - playerPos.y) / 10.0);
                    return half4(yNorm, 0, 1.0 - yNorm, 1.0);
                }

                // Height filter: keep ground (at or below player Y + offset)
                if (worldPos.y <= playerPos.y + _HeightOffset)
                    return color;

                // XZ distance
                float dist = length(worldPos.xz - playerPos.xz);

                // Cutout fade
                float alpha = saturate((dist - radius) / max(fadeWidth, 0.001));
                color.rgb *= alpha;

                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
