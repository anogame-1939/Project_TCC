Shader "Hidden/Kino/AnalogGlitchURP"
{
    Properties {
        _ScanLineJitter("ScanLineJitter", Vector) = (0,1,0,0) // x=disp, y=threshold
        _VerticalJump  ("VerticalJump",  Vector) = (0,0,0,0) // x=amp,  y=time
        _HorizontalShake("HorizontalShake", Float) = 0
        _ColorDrift    ("ColorDrift",    Vector) = (0,0,0,0) // x=amount, y=time
    }
    SubShader {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        ZWrite Off ZTest Always Cull Off

        Pass {
            Name "AnalogGlitchURP"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float2 _ScanLineJitter;
            float2 _VerticalJump;
            float  _HorizontalShake;
            float2 _ColorDrift;

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_LinearClamp);

            struct VOut {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            // ★ Blitter 用：頂点IDから全画面頂点/UVを生成
            VOut Vert(uint vertexID : SV_VertexID)
            {
                VOut o;
                o.positionHCS = GetFullScreenTriangleVertexPosition(vertexID);
                o.uv          = GetFullScreenTriangleTexCoord(vertexID);
                return o;
            }

            float4 SampleSrc(float2 uv) {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }

            float4 Frag(VOut i) : SV_Target
            {
                float2 uv = i.uv;

                // scanline jitter
                float sl = frac(uv.y * 480.0);
                float glitch = step(_ScanLineJitter.y, abs(frac(sin(sl*43758.5453)*0.5+0.5)-0.5)*2.0);
                uv.x += _ScanLineJitter.x * glitch;

                // vertical jump
                uv.y = frac(uv.y + (sin(_VerticalJump.y)*_VerticalJump.x*0.05));

                // horizontal shake
                uv.x += (_HorizontalShake * (sin(_Time.y*20.0)*0.005));

                // color drift（RGB のうち R/G をずらす）
                float2 cd = float2(_ColorDrift.x * sin(_ColorDrift.y), 0);
                float4 col;
                col.r = SampleSrc(uv + cd).r;
                col.g = SampleSrc(uv - cd).g;
                col.b = SampleSrc(uv).b;
                col.a = 1;
                return col;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
