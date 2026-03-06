Shader "Custom/PostProcess/CircleCutout"
{
    Properties
    {
        [Header(Circle Cutout)]
        _CutoutRadius ("Cutout Radius", Range(0, 1)) = 0.15
        _CutoutFadeWidth ("Fade Width", Range(0, 0.5)) = 0.05
        _PlayerScreenPos ("Player Screen Pos (viewport 0-1)", Vector) = (0.5, 0.5, 0, 0)

        [Header(Source Select)]
        [Toggle] _UseGlobalParams ("Use Global Params", Float) = 1

        [Header(Debug)]
        [KeywordEnum(OFF, DEPTH_RAW, BACKGROUND, DISTANCE, SOLID)] _Debug ("Debug View", Float) = 0
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

            float4 _PlayerScreenPos;
            float  _CutoutRadius;
            float  _CutoutFadeWidth;
            float  _UseGlobalParams;
            float  _Debug;

            float4 _Global_PlayerScreenPos;
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
                float2 playerUV  = _UseGlobalParams > 0.5 ? _Global_PlayerScreenPos.xy : _PlayerScreenPos.xy;
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

                // Screen-space distance (aspect ratio corrected)
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 diff = uv - playerUV;
                diff.x *= aspect; // correct for non-square screens
                float dist = length(diff);

                // Debug: distance visualization
                if (_Debug > 2.5 && _Debug < 3.5)
                {
                    float vis = saturate(dist / 0.5);
                    float edge = 1.0 - smoothstep(radius - 0.002, radius + 0.002, dist);
                    return half4(vis, vis * 0.5, edge, 1.0);
                }

                // Debug: SOLID - inside circle = magenta, outside = normal
                if (_Debug > 3.5 && _Debug < 4.5)
                {
                    float t_dbg = saturate((dist - radius) / max(fadeWidth, 0.001));
                    return lerp(half4(1.0, 0.0, 1.0, 1.0), color, t_dbg);
                }

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
