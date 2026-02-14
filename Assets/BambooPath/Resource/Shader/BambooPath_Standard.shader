Shader "BambooPath/BambooPath_Standard"
{
    Properties
    {
        [Header(Parameters)]
        _UVScale("UV Scale",float) = 1
        _ColorTint("Color Tint",Color) = (1,1,1,1)
        _EmissionTint("Emission Tint",Color) = (1,1,1,1)
        _AOMin("AO Min",Range(0,1)) = 0.3
        _RoughnessAjust("Roughness Ajustment",Range(-1,1)) = 0

        [Space(20)]

        [Header(Maps)]
        [NoScaleOffset]_ColorMap("Basecolor",2D) = "white"
        [NoScaleOffset]_ORMMap("ORM",2D) = "white"
        [NoScaleOffset]_NormalMap("Normal",2D) = "bump"
        [NoScaleOffset]_EmissionMap("Emission",2D) = "black"
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        float _UVScale;
        fixed3 _ColorTint;
        half _AOMin;
        half _RoughnessAjust;
        fixed3 _EmissionTint;

        sampler2D _ColorMap;
        sampler2D _ORMMap;
        sampler2D _NormalMap;
        sampler2D _EmissionMap;


        struct Input
        {
            float2 uv_ColorMap;
        };


        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)


        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_ColorMap * _UVScale;

            half3 orm = tex2D(_ORMMap, uv);

            fixed3 color = tex2D(_ColorMap, uv) * _ColorTint;
            half ao = lerp(_AOMin,1, orm.r);
            half roughness = saturate(orm.g + _RoughnessAjust);
            half metallic = orm.b;
            float3 normal = UnpackNormal(tex2D(_NormalMap, uv));
            fixed3 emission = tex2D(_EmissionMap, uv) * _EmissionTint;

            o.Albedo = color;
            o.Occlusion = ao;
            o.Smoothness = 1 - roughness;
            o.Metallic = metallic;
            o.Normal = normal;
            o.Emission = emission;
        }


        ENDCG
    }
    FallBack "Standard"
}
