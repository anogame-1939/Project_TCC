Shader "NovelEditor/BlurEffect"
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
            fixed4 _MainTex_TexelSize;
            float _Strength;

            fixed4 frag (v2f i) : COLOR
            {
                _Strength = max(1,_Strength*10);
                float weight_total = 0;

                fixed4 col = (0, 0, 0, 0);

                [loop]
                for (float x = -_Strength; x <= _Strength; x += 1)
                {
                    float distance_normalized = abs(x / _Strength);
                    float weight = exp(-0.5 * pow(distance_normalized, 2) * 5.0);
                    weight_total += weight;
                    col += tex2D(_MainTex, i.uv + float2(x * _MainTex_TexelSize.x, 0))* weight;
                }
                col /= weight_total;
                return col;
            }
            ENDCG
        }
    }
}                