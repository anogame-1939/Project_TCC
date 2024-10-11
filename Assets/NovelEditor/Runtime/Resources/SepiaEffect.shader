Shader "NovelEditor/SepiaEffect"
{
	Properties
	{
		_MainTex("Sprite Texture", 2D) = "white" {}
        _Strength("Strength", float) = 5
	}
	SubShader
    {
        Tags { "QUEUE"="Transparent" "RenderType"="Transparent" }
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
                float4 color    : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4  color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            sampler2D _MainTex;
            float _Strength;

            fixed4 frag (v2f i) : COLOR
            {
				_Strength*=0.01;
				fixed4 col = tex2D(_MainTex, i.uv)*i.color;
				float gray = col.r * 0.3 + col.g * 0.6 + col.b * 0.1 - _Strength;
				gray = (gray < 0) ? 0 : gray;

				float R = gray + _Strength;
				float B = gray - _Strength;

				R = (R > 1.0) ? 1.0 : R;
				B = (B < 0) ? 0 : B;
				col.rgb = fixed3(R, gray, B);
                return col;
            }
            ENDCG
        }
    }
}

 