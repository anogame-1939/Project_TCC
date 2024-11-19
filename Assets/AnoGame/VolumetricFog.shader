Shader "Custom/VolumetricFog"
{
    Properties
    {
        _FogDensity ("Fog Density", Range(0, 1)) = 0.1
        _FogStart ("Fog Start Distance", Float) = 0
        _FogEnd ("Fog End Distance", Float) = 100
        _FogColor ("Fog Color", Color) = (0.5, 0.5, 0.5, 1)
        _Steps ("Ray March Steps", Range(1, 128)) = 64
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };
            
            float _FogDensity;
            float _FogStart;
            float _FogEnd;
            float4 _FogColor;
            float _Steps;
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }
            
            float SampleDensity(float3 position)
            {
                // ここでフォグの密度をサンプリング
                // 高さベースのフォグや、ノイズテクスチャによる変調などを実装可能
                float heightFalloff = exp(-max(position.y, 0) * 0.2);
                return _FogDensity * heightFalloff;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // カメラ位置を取得
                float3 cameraPos = _WorldSpaceCameraPos;
                float3 rayStart = cameraPos;
                float3 rayDir = normalize(input.positionWS - cameraPos);
                
                // レイマーチングの設定
                float stepSize = (_FogEnd - _FogStart) / _Steps;
                float3 stepVector = rayDir * stepSize;
                float3 currentPos = rayStart + rayDir * _FogStart;
                
                // 累積フォグ値の計算
                float transmittance = 1;
                float3 accumFog = 0;
                
                for (int i = 0; i < _Steps; i++)
                {
                    float density = SampleDensity(currentPos);
                    float stepTransmittance = exp(-density * stepSize);
                    
                    // ライティングの計算
                    Light mainLight = GetMainLight();
                    float3 lightColor = mainLight.color * mainLight.distanceAttenuation;
                    
                    // フォグに対するライティングの影響を追加
                    float3 stepFog = _FogColor.rgb * lightColor * density;
                    
                    accumFog += stepFog * transmittance * (1 - stepTransmittance) * stepSize;
                    transmittance *= stepTransmittance;
                    
                    currentPos += stepVector;
                    
                    // 早期終了チェック
                    if (transmittance < 0.01) break;
                }
                
                return float4(accumFog, 1 - transmittance);
            }
            ENDHLSL
        }
    }
}