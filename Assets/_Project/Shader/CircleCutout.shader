Shader "Custom/PostProcess/CircleCutout"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "CircleCutoutPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Global properties (set from C# via Shader.SetGlobal*)
            float4 _PlayerWorldPos;
            float  _CutoutRadius;
            float  _CutoutFadeWidth;

            half4 Frag(Varyings input) : SV_Target
            {
                // Sample the original color
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);

                // Passthrough when effect is disabled (radius <= 0)
                if (_CutoutRadius <= 0.0 && _CutoutFadeWidth <= 0.0)
                    return color;

                // Sample depth
                float2 uv = input.texcoord;
                float depth = SampleSceneDepth(uv);

                // Skip skybox (far plane) - keep original color
                #if UNITY_REVERSED_Z
                    if (depth < 0.0001)
                        return color;
                #else
                    if (depth > 0.9999)
                        return color;
                #endif

                // Reconstruct world position from depth using URP's built-in function
                float3 worldPos = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);

                // Calculate XZ distance to player
                float2 diff = worldPos.xz - _PlayerWorldPos.xz;
                float dist = length(diff);

                // Apply cutout with fade edge
                // innerRadius: fully transparent
                // outerRadius: fully opaque
                float innerRadius = _CutoutRadius;
                float outerRadius = _CutoutRadius + _CutoutFadeWidth;
                float alpha = saturate((dist - innerRadius) / max(_CutoutFadeWidth, 0.001));

                // Fade the color (black in cutout area)
                color.rgb *= alpha;

                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
