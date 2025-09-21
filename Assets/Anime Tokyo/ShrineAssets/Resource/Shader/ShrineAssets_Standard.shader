Shader "ShrineAssets/ShrineAssets_Standard"
{
    Properties
    {
        [Header(BaseColor)] [Space(10)]
        _ColorTint("ColorTint",Color) = (1,1,1,1)
        [NoScaleOffset]_BasecolorMap("BasecolorMap",2D) = "white"{}
        
        [Header(Metallic)][Space(10)]
        [NoScaleOffset] _MetallicMap("MetallicMap",2D) = "black"{}
        _MetallicValue("MetallicValue",Range(0,1)) = 0
        [KeywordEnum(TEXTURE,VALUE)] _MetallicSource("MetallicSource",float) = 0
        [Space(20)]

        [Header(Roughness)][Space(10)]
        [NoScaleOffset] _RoughnessMap("RoughnessMap",2D) = "white"{}
        _RoughnessValue("RoughnessValue",Range(0,1)) = 0.8
        [KeywordEnum(TEXTURE,VALUE)] _RoughnessSource("RoughnessSource",float) = 0
        [Space(20)]

        [Header(Normal)][Space(10)]
        [Normal][NoScaleOffset] _NormalMap("NormalMap",2D) = "bump"{}
        _NormalFlatness("NormalMapFlatness",Range(0,1)) = 0

        [Header(UV)][Space(10)]
        _UV_Scale("UVScale",float) = 1
        _UV_OffsetX("offsetX",float) = 0
        _UV_OffsetY("offsetY",float) = 0
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows

        #pragma target 3.0

        #pragma shader_feature _METALLICSOURCE_TEXTURE _METALLICSOURCE_VALUE
        #pragma shader_feature _ROUGHNESSSOURCE_TEXTURE _ROUGHNESSSOURCE_VALUE


        fixed4 _ColorTint;
        sampler2D _BasecolorMap;

        sampler2D _MetallicMap;
        half _MetallicValue;

        sampler2D _RoughnessMap;
        half _RoughnessValue;

        sampler2D _NormalMap;
        half _NormalFlatness;

        float _UV_Scale;
        float _UV_OffsetX;
        float _UV_OffsetY;

        struct Input
        {
            float2 uv_BasecolorMap;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            //uv
            float2 uv = IN.uv_BasecolorMap;
            uv.x += _UV_OffsetX;
            uv.y += _UV_OffsetY;
            uv *= _UV_Scale;

            //Color
            fixed4 color = tex2D(_BasecolorMap, uv) * _ColorTint;


            //Metallic
            #ifdef _METALLICSOURCE_TEXTURE
                half metallic = tex2D(_MetallicMap, uv);
            #elif _METALLICSOURCE_VALUE
                half metallic = _MetallicValue;
            #endif


            //Roughness(Smoothness)
            #ifdef _ROUGHNESSSOURCE_TEXTURE
                half roughness = 1 - tex2D(_RoughnessMap, uv);
            #elif _ROUGHNESSSOURCE_VALUE
                half roughness = 1 - _RoughnessValue;
            #endif
           

            //Normal
            half3 normalMap = UnpackNormal(tex2D(_NormalMap, uv));
            
            half3 normal = lerp(normalMap,half3(0,0,1),_NormalFlatness);
            


            //Result
            o.Albedo = color.rgb;
            o.Metallic = metallic;
            o.Smoothness = roughness;
            o.Normal = normal;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
