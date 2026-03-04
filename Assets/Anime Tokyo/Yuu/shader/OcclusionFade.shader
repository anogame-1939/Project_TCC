Shader "Custom/OcclusionFade"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _Radius("Radius", Float) = 1.5
        _Softness("Softness", Float) = 0.5
    }

        SubShader
        {
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

            Pass
            {
                Blend SrcAlpha OneMinusSrcAlpha
                ZWrite Off

                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                sampler2D _BaseMap;
                float4 _PeekPlayerPos;
                float _Radius;
                float _Softness;

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 pos : SV_POSITION;
                    float3 worldPos : TEXCOORD1;
                };

                v2f vert(appdata v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    return o;
                }

                half4 frag(v2f i) : SV_Target
                {
                    float dist = distance(i.worldPos.xz, _PeekPlayerPos.xz);

                    float mask = smoothstep(_Radius, _Radius + _Softness, dist);

                    half4 col = tex2D(_BaseMap, i.uv);
                    col.a *= mask;

                    return col;
                }
                ENDHLSL
            }
        }
}