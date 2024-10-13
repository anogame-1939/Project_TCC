Shader "NovelEditor/NoiseEffect"
{
    Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Strength("Strength", float) = 5
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
            #pragma multi_compile DUMMY PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color : COLOR;
                half2 uv  : TEXCOORD0;
            };

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.uv;
                OUT.color = IN.color;

                return OUT;
            }

            float rand(float3 co)
            {
                return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
            }

            sampler2D _MainTex;
            float _Strength;

            static const float division = 768;
            static const float blackinterval = 6;
            static const float blackheight = 0.5;

            fixed4 frag(v2f i) : COLOR
            {
                _Strength *=0.01;
                int divisionindex = i.uv.y * division;

                int noiseindex = divisionindex / blackinterval;

                float3 timenoise = float3(0, int(_Time.x * 61), int(_Time.x * 83));
                float noiserate = rand(timenoise) < 0.05 ? 10 : 1;

                float xnoise = rand(float3(noiseindex, 0, 0) + timenoise);
                xnoise = xnoise * xnoise - 0.5; 
                xnoise = xnoise * _Strength * noiserate;
                xnoise = xnoise * (_SinTime.w / 2 + 1.1); 
                xnoise = xnoise + (abs((int(_Time.x * 2000) % int(division / blackinterval)) - noiseindex) < 5 ? 0.005 : 0); 

                float2 uv = i.uv + float2(xnoise, 0);

                fixed4 col1 = tex2D(_MainTex, uv);
                fixed4 col2 = tex2D(_MainTex, uv + float2(0.005, 0));
                fixed4 col3 = tex2D(_MainTex, uv + float2(-0.005, 0));
                fixed4 col4 = tex2D(_MainTex, uv + float2(0, 0.005));
                fixed4 col5 = tex2D(_MainTex, uv + float2(0,-0.005));
                fixed4 col = (col1 * 4 + col2 + col3 + col4 + col5) / 8;

                col.rgb = divisionindex % blackinterval < blackheight ? float4(0, 0, 0, 1) : col.rgb;
                col *= i.color;
                return col;
            }

        ENDCG
        }
    }
}
