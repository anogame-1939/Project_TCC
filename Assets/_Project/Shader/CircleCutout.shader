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

                // Sample depth
                float depth = SampleSceneDepth(input.texcoord);

                // Skip skybox (far plane)
                #if UNITY_REVERSED_Z
                    if (depth < 0.0001)
                        return color;
                #else
                    if (depth > 0.9999)
                        return color;
                #endif

                // Reconstruct world position from depth
                float2 uv = input.texcoord;
                #if UNITY_UV_STARTS_AT_TOP
                    uv.y = 1.0 - uv.y;
                #endif

                float4 ndc = float4(uv * 2.0 - 1.0, depth, 1.0);

                #if UNITY_REVERSED_Z
                    ndc.z = depth;
                #else
                    ndc.z = depth * 2.0 - 1.0;
                #endif

                float4 worldPos = mul(UNITY_MATRIX_I_VP, ndc);
                worldPos.xyz /= worldPos.w;

                // Calculate XZ distance to player
                float2 diff = worldPos.xz - _PlayerWorldPos.xz;
                float dist = length(diff);

                // Apply cutout with fade edge
                // innerRadius: fully transparent
                // outerRadius: fully opaque
                float innerRadius = _CutoutRadius;
                float outerRadius = _CutoutRadius + _CutoutFadeWidth;
                float alpha = saturate((dist - innerRadius) / max(_CutoutFadeWidth, 0.001));

                // Lerp to transparent (checkerboard or just fade to background)
                color.rgb *= alpha;
                color.a = alpha;

                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
