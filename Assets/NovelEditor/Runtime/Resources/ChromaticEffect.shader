Shader "NovelEditor/ChromaticEffect"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _FrameRate("FrameRate", Range(0.1,30)) = 26.4
        _Strength("Strength", float) = 5

        _LineColor("LineColor", Color) = (0,0,0,0)
        _LineSpeed("LineSpeed",Range(0,10)) = 2.6
        _LineSize("LineSize",Range(0,1)) = 0.01
        _ColorGap("ColorGap",Range(0,1.0)) = 0.1
        _Alpha("Alpha", Range(0,1)) = 0.78
        _GlitchScale("GlitchScale", Range(1,10)) = 2.0
    }
        SubShader
        {
            Tags
            {
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "RenderType" = "Transparent"
                "PreviewType" = "Plane"
                "CanUseSpriteAtlas" = "True"
            }

            Cull Off
            Lighting Off
            ZWrite Off
            Fog { Mode Off }
            Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    float2 line_uv : TEXCOORD1;
                    float4 color    : COLOR;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float2 line_uv : TEXCOORD1;
                    float4 vertex : SV_POSITION;
                    float4 color    : COLOR;
                };

                sampler2D _MainTex;
                float _FrameRate;
                float _Strength;

                float4 _MainTex_ST;
                float4 _LineColor;
                float _LineSpeed;
                float _LineSize;
                float _ColorGap;
                float _Alpha;
                float _GlitchScale;

                float rand(float2 co) 
                {
                    return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453);
                }

                float perlinNoise(fixed2 st)
                {
                    fixed2 p = floor(st);
                    fixed2 f = frac(st);
                    fixed2 u = f * f * (3.0 - 2.0 * f);

                    float v00 = rand(p + fixed2(0, 0));
                    float v10 = rand(p + fixed2(1, 0));
                    float v01 = rand(p + fixed2(0, 1));
                    float v11 = rand(p + fixed2(1, 1));

                    return lerp(lerp(dot(v00, f - fixed2(0, 0)), dot(v10, f - fixed2(1, 0)), u.x),
                                lerp(dot(v01, f - fixed2(0, 1)), dot(v11, f - fixed2(1, 1)), u.x),
                                u.y) + 0.5f;
                }

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    o.color = v.color;
                    o.line_uv.y = v.line_uv.y - _Time.z * _LineSpeed;
                    return o;
                }

                fixed4 frag(v2f i) : COLOR
                {
                    float2 uv = i.uv;

                    float r = tex2D(_MainTex, uv + _ColorGap * perlinNoise(_Time.z)).r;
                    float b = tex2D(_MainTex, uv - _ColorGap * perlinNoise(_Time.z)).b;
                    float2 ga = tex2D(_MainTex, uv).ga;
                    float4 shiftColor = fixed4(r, ga.x, b, ga.y);

                    float interpolation = step(frac(i.line_uv.y * 15), _LineSize);
                    float4 noiseLineColor = lerp(shiftColor, _LineColor, interpolation);

                    float posterize1 = floor(frac(perlinNoise(_SinTime) * 10) / (1 / _FrameRate)) * (1 / _FrameRate);
                    float posterize2 = floor(frac(perlinNoise(_SinTime) * 5) / (1 / _FrameRate)) * (1 / _FrameRate);
                    float noiseX = (2.0 * rand(posterize1) - 0.5) * 0.1;
                    float strength = step(rand(posterize2), _Strength*0.1);
                    noiseX *= strength;
                    float noiseY = 2.0 * rand(posterize1) - 0.5;
                    float glitchLine1 = step(uv.y - noiseY, rand(uv));
                    float glitchLine2 = step(uv.y + noiseY, noiseY);
                    float glitch = saturate(glitchLine1 - glitchLine2);
                    uv.x = lerp(uv.x, uv.x + noiseX, glitch);
                    float4 glitchColor = tex2D(_MainTex, uv);
                    float4 finalColor = noiseLineColor * glitchColor;
                    finalColor*=i.color;
                    
                    return finalColor;
                }
                ENDCG
            }
        }
}