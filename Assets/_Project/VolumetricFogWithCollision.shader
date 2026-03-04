Shader "Custom/VolumetricFogWithCollision"
{
    Properties
    {
        _FogDensity ("Fog Density", Range(0, 1)) = 0.1
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
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
            
            // シーンの深度を取得する関数
            float GetSceneDepth(float2 screenUV)
            {
                float2 uv = screenUV;
                #if UNITY_REVERSED_Z
                    float depth = SampleSceneDepth(uv);
                #else
                    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
                #endif
                return LinearEyeDepth(depth, _ZBufferParams);
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // スクリーン座標を計算
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneDepth = GetSceneDepth(screenUV);
                
                // カメラ位置と方向を取得
                float3 cameraPos = _WorldSpaceCameraPos;
                float3 rayDir = normalize(input.positionWS - cameraPos);
                
                // レイマーチングの設定
                float rayLength = min(sceneDepth, length(input.positionWS - cameraPos));
                float stepSize = rayLength / _Steps;
                float3 stepVector = rayDir * stepSize;
                float3 currentPos = cameraPos;
                
                // フォグの蓄積
                float transmittance = 1.0;
                float3 accumFog = 0;
                
                for (int i = 0; i < _Steps; i++)
                {
                    // 現在位置がシーンのデプスを超えていないかチェック
                    float currentDepth = length(currentPos - cameraPos);
                    if (currentDepth > sceneDepth) break;
                    
                    // フォグの密度計算
                    float density = _FogDensity;
                    
                    // 高さベースの密度変調（オプション）
                    float heightFalloff = exp(-max(currentPos.y, 0) * 0.2);
                    density *= heightFalloff;
                    
                    // ライティング計算
                    Light mainLight = GetMainLight();
                    float3 lightContrib = mainLight.color * mainLight.distanceAttenuation;
                    
                    // フォグの蓄積
                    float stepTransmittance = exp(-density * stepSize);
                    float3 stepFog = _FogColor.rgb * lightContrib * density;
                    
                    accumFog += stepFog * transmittance * (1 - stepTransmittance) * stepSize;
                    transmittance *= stepTransmittance;
                    
                    // 次のステップへ
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