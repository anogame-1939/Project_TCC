Shader "NovelEditor/MosaicEffect"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		_Strength("Strength",float) = 5
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

				half2 uv = floor(i.uv * _Strength*5) / (_Strength*5);
				fixed4 col = tex2D(_MainTex, uv)*i.color;
                return col;
            }
            ENDCG
        }
    }
}

