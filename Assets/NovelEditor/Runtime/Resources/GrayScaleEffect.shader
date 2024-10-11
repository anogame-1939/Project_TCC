Shader "NovelEditor/GrayScaleEffect"
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
                _Strength*=0.05;
                fixed4 col = tex2D(_MainTex, i.uv)*i.color;
                col.rgb = dot(col.rgb, float3(0.3, 0.59, 0.11)*_Strength);
                return col;
            }
            ENDCG
        }
    }
}
                