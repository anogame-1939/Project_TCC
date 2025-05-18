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
        _NoiseOctaves ("Noise Octaves", Int) = 2
        _Quality ("Quality", Range(0.1, 1)) = 1
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.5
        [Toggle] _UseGradient ("Use Gradient", Float) = 1
        _TopColor ("Top Color", Color) = (1, 1, 1, 1)
        _BottomColor ("Bottom Color", Color) = (0.8, 0.8, 0.8, 1)
        _GradientStrength ("Gradient Strength", Range(0, 1)) = 0.5
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
            #pragma multi_compile_local _ _USE_GRADIENT
            
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

            // 新規変数
            int _NoiseOctaves;
            float _Quality;
            float _EdgeSoftness;
            float _UseGradient;
            float4 _TopColor;
            float4 _BottomColor;
            float _GradientStrength;
            
            float SampleNoise(float3 pos, float3 timeOffset)
            {
                float3 samplePos = pos * _NoiseScale * _Quality; // クオリティを反映
                samplePos += _NoiseSpeed.xyz;
                return SAMPLE_TEXTURE3D(_NoiseTex, sampler_NoiseTex, frac(samplePos)).r;
            }
            
            float3 FlowMovement(float3 worldPos, float3 timeOffset)
            {
                float3 flowOffset = _FlowDirection.xyz * frac(_Time.y * 0.1);
                float3 pos = worldPos + flowOffset;
                
                float baseNoise = SampleNoise(pos * 2.0, timeOffset);
                float turbulence = baseNoise * _Turbulence;
                
                float3 distortion = float3(
                    SampleNoise(pos + float3(0, turbulence, 0), timeOffset),
                    SampleNoise(pos + float3(turbulence, 0, 0), timeOffset),
                    SampleNoise(pos + float3(0, 0, turbulence), timeOffset)
                ) * _DistortionStrength;
                
                return pos + distortion;
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
                float3 flowPos = FlowMovement(input.worldPos, _NoiseSpeed.xyz);
                
                // パフォーマンス最適化されたノイズ合成
                float noise = 0;
                float amplitude = 1.0;
                float frequency = 1.0;
                
                UNITY_LOOP
                for(int i = 0; i < _NoiseOctaves; i++)
                {
                    noise += SampleNoise(flowPos * frequency, _NoiseSpeed.xyz) * amplitude;
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                // ソフトエッジ処理
                float fogStrength = lerp(
                    saturate(noise * _FogDensity),
                    smoothstep(0.0, 1.0, noise * _FogDensity),
                    _EdgeSoftness
                );
                
                // 高さによるフェード
                float heightFade = saturate(1.0 - (input.worldPos.y * 0.1));
                fogStrength *= heightFade;
                
                // ライティング
                Light mainLight = GetMainLight();
                float3 lightContribution = mainLight.color * mainLight.distanceAttenuation;
                
                // グラデーション処理
                float4 finalColor = _FogColor;
                if (_UseGradient > 0.5)
                {
                    float gradientFactor = saturate(input.worldPos.y * 0.1);
                    finalColor = lerp(_BottomColor, _TopColor, gradientFactor * _GradientStrength);
                }
                
                finalColor.rgb *= lightContribution;
                finalColor.a = fogStrength;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}