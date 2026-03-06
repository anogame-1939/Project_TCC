Shader "Custom/CastShadowSprite"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor]   _Color ("Color", Color) = (1, 1, 1, 1)
        _SkyColor ("Sky Color Influence", Range(0, 1)) = 0
        _ShadowColor ("Shadow Color", Color) = (0.3, 0.3, 0.5, 1)
        _ShadowIntensity ("Shadow Intensity", Range(0, 1)) = 0.6

        [Header(Depth Override)]
        [Toggle] _UseFootDepth ("Use Foot Depth", Float) = 1
        _DepthBias ("Depth Bias (push toward camera)", Range(0, 0.01)) = 0.001

        [Header(Debug)]
        [KeywordEnum(OFF, FOOT_DEPTH, ACTUAL_DEPTH, DIFF)] _DepthDebug ("Depth Debug", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
            "RenderType" = "TransparentCutout"
        }

        // =============================================
        // Pass 1: Forward -- テクスチャ × Ambient × Color × Shadow
        // =============================================
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // Shadow keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half   _SkyColor;
                half4  _ShadowColor;
                half   _ShadowIntensity;
                float  _UseFootDepth;
                float  _DepthBias;
                float  _DepthDebug;
            CBUFFER_END

            struct VIn
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR; // SpriteRenderer vertex color
            };

            struct VOut
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half4  vertexColor : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
                float  footDepth   : TEXCOORD4;
                float  actualDepth : TEXCOORD5;
            };

            VOut Vert(VIn i)
            {
                VOut o;
                VertexPositionInputs posInputs = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionHCS = posInputs.positionCS;
                o.positionWS  = posInputs.positionWS;
                o.uv          = TRANSFORM_TEX(i.uv, _MainTex);
                o.vertexColor = i.color;
                o.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);

                // 実際の深度
                o.actualDepth = posInputs.positionCS.z / posInputs.positionCS.w;

                // 足元（オブジェクト原点）の深度を計算
                float4 footCS = TransformObjectToHClip(float3(0, 0, 0));
                o.footDepth = footCS.z / footCS.w;

                return o;
            }

            half4 Frag(VOut i, out float depth : SV_Depth) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                clip(texColor.a - 0.5);

                // 深度上書き + バイアス（カメラ方向に押し出す）
                float baseDepth = _UseFootDepth > 0.5 ? i.footDepth : i.actualDepth;
                #if UNITY_REVERSED_Z
                    depth = baseDepth + _DepthBias; // Reversed-Z: 大きい値 = 手前
                #else
                    depth = baseDepth - _DepthBias; // Normal-Z: 小さい値 = 手前
                #endif

                // デバッグ表示
                if (_DepthDebug > 0.5 && _DepthDebug < 1.5)
                {
                    // FOOT_DEPTH: 足元深度をグレースケールで表示
                    return half4(i.footDepth, i.footDepth, i.footDepth, 1);
                }
                if (_DepthDebug > 1.5 && _DepthDebug < 2.5)
                {
                    // ACTUAL_DEPTH: 実際の深度をグレースケールで表示
                    return half4(i.actualDepth, i.actualDepth, i.actualDepth, 1);
                }
                if (_DepthDebug > 2.5)
                {
                    // DIFF: 差分表示（足元が浅い=緑、深い=赤）
                    float diff = (i.footDepth - i.actualDepth) * 100.0;
                    return half4(saturate(diff), saturate(-diff), 0, 1);
                }

                // Ambient 疑似ライティング（既存ロジック再現）
                // lerp(AmbientSkyColor, white, 1-SkyColor)
                half3 ambientSky = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
                half3 ambientTint = lerp(ambientSky, half3(1, 1, 1), 1.0 - _SkyColor);

                // 色の合成
                half3 baseColor = texColor.rgb * ambientTint * _Color.rgb * i.vertexColor.rgb;

                // ★ 影の受信
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half shadowAtten = mainLight.shadowAttenuation;

                // Shadow color blend
                // shadowAtten=1 (影なし) → そのまま
                // shadowAtten=0 (完全な影) → _ShadowColor で暗くする
                half3 shadowTint = lerp(_ShadowColor.rgb, half3(1, 1, 1), shadowAtten);
                half3 finalColor = baseColor * lerp(half3(1, 1, 1), shadowTint, _ShadowIntensity);

                // Fog
                finalColor = MixFog(finalColor, i.fogFactor);

                return half4(finalColor, texColor.a * i.vertexColor.a);
            }
            ENDHLSL
        }

        // =============================================
        // Pass 2: ShadowCaster -- 影を投げる
        // =============================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half   _SkyColor;
                half4  _ShadowColor;
                half   _ShadowIntensity;
                float  _UseFootDepth;
                float  _DepthBias;
                float  _DepthDebug;
            CBUFFER_END

            float3 _LightDirection;

            struct ShadowVIn
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct ShadowVOut
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            ShadowVOut ShadowVert(ShadowVIn i)
            {
                ShadowVOut o;
                float3 posWS = TransformObjectToWorld(i.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(i.normalOS);

                // Shadow bias
                float4 posCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    posCS.z = min(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    posCS.z = max(posCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                o.positionHCS = posCS;
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }

            half4 ShadowFrag(ShadowVOut i) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                clip(alpha - 0.5);
                return 0;
            }
            ENDHLSL
        }

        // =============================================
        // Pass 3: DepthOnly -- 深度パス
        // =============================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half   _SkyColor;
                half4  _ShadowColor;
                half   _ShadowIntensity;
                float  _UseFootDepth;
                float  _DepthOffsetOS;
                float  _DepthDebug;
            CBUFFER_END

            struct DepthVIn
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct DepthVOut
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float  footDepth   : TEXCOORD1;
            };

            DepthVOut DepthVert(DepthVIn i)
            {
                DepthVOut o;
                o.positionHCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);

                // 足元基準の深度
                float4 footCS = TransformObjectToHClip(float3(0, 0, 0));
                o.footDepth = footCS.z / footCS.w;

                return o;
            }

            half4 DepthFrag(DepthVOut i, out float depth : SV_Depth) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                clip(alpha - 0.5);
                depth = i.footDepth;
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
