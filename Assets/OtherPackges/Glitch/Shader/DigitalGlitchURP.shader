Shader "Hidden/Kino/DigitalGlitchURP"
{
    Properties {
        _Intensity("Intensity", Range(0,1)) = 0
        _NoiseTex("Noise", 2D) = "white" {}
        _TrashTex("Trash", 2D) = "black" {}
    }
    SubShader {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        ZWrite Off ZTest Always Cull Off

        Pass {
            Name "DigitalGlitchURP"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _Intensity;
            TEXTURE2D_X(_BlitTexture);      SAMPLER(sampler_LinearClamp);
            TEXTURE2D(_NoiseTex);           SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_TrashTex);           SAMPLER(sampler_TrashTex);

            struct VOut {
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

            float rand(float2 co){ return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453); }

            float4 Frag(VOut i) : SV_Target
            {
                float2 uv = i.uv;

                // ノイズでUVをブロック歪み
                float2 n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, float2(uv.x, floor(uv.y*10)/10)).rg;
                float glitch = _Intensity;
                uv += (n - 0.5) * 0.05 * glitch;

                // ランダムにライン飛び
                if (rand(float2(_Time.y, floor(uv.y*200))) < glitch*0.5)
                    uv.x += (rand(float2(uv.y, _Time.y)) - 0.5) * 0.2 * glitch;

                // 旧作の TrashTex を合成
                float4 src   = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float4 trash = SAMPLE_TEXTURE2D(_TrashTex, sampler_TrashTex, uv);
                return lerp(src, trash, glitch * 0.15);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
