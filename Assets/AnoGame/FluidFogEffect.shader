Shader "Custom/FluidFogEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 3D) = "white" {}
        _FogColor ("Fog Color", Color) = (0.8, 0.8, 0.8, 1)
        _FogDensity ("Fog Density", Range(0, 2)) = 1.0
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 2.0
        _NoiseSpeed ("Noise Speed", Vector) = (0.1, 0.2, 0.1, 0)
        _FlowDirection ("Flow Direction", Vector) = (1, 0, 0, 0)
        _Turbulence ("Turbulence", Range(0, 2)) = 1.0
        _DistortionStrength ("Distortion Strength", Range(0, 1)) = 0.1
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
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
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE3D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            
            float4 _FogColor;
            float _FogDensity;
            float _NoiseScale;
            float4 _NoiseSpeed;
            float4 _FlowDirection;
            float _Turbulence;
            float _DistortionStrength;
            
            // 3Dノイズをサンプリング（修正版）
            float SampleNoise(float3 pos, float time)
            {
                // frac関数を使用して周期的な動きを実現
                float3 samplePos = pos * _NoiseScale;
                samplePos += frac(_NoiseSpeed.xyz * time);
                return SAMPLE_TEXTURE3D(_NoiseTex, sampler_NoiseTex, frac(samplePos)).r;
            }
            
            // 流体の動きを計算（修正版）
            float3 FlowMovement(float3 worldPos, float time)
            {
                // フローの基本移動
                float3 flowOffset = _FlowDirection.xyz * frac(time * 0.1);
                float3 pos = worldPos + flowOffset;
                
                // 乱流の計算
                float turbulence = SampleNoise(pos * 2.0, time * 0.5) * _Turbulence;
                pos += float3(
                    SampleNoise(pos + float3(0, turbulence, 0), time),
                    SampleNoise(pos + float3(turbulence, 0, 0), time),
                    SampleNoise(pos + float3(0, 0, turbulence), time)
                ) * _DistortionStrength;
                
                return pos;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                float3 flowPos = FlowMovement(input.worldPos, time);
                
                // 複数レイヤーのノイズを合成
                float noise = 0;
                float amplitude = 1.0;
                float frequency = 1.0;
                
                for(int i = 0; i < 3; i++)
                {
                    noise += SampleNoise(flowPos * frequency, time) * amplitude;
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                // エッジの柔らかさを調整
                float fogStrength = saturate(noise * _FogDensity);
                
                // 高さによるフェード
                float heightFade = saturate(1.0 - (input.worldPos.y * 0.1));
                fogStrength *= heightFade;
                
                // ライティングの影響を計算
                Light mainLight = GetMainLight();
                float3 lightContribution = mainLight.color * mainLight.distanceAttenuation;
                
                float4 finalColor = _FogColor;
                finalColor.rgb *= lightContribution;
                finalColor.a = fogStrength;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}